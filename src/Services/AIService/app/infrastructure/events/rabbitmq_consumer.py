import json
import logging
from typing import Optional

import aio_pika
from aio_pika import Connection, Channel, Queue, IncomingMessage
from aio_pika.abc import AbstractIncomingMessage
from aiormq.exceptions import ChannelNotFoundEntity

from app.infrastructure.config.settings import settings
from app.core.snapshot.events import ClassroomEvent, ClassroomSnapshotEventHandler

logger = logging.getLogger(__name__)


class ClassroomProgressEventConsumer:
    def __init__(
        self,
        event_handler: ClassroomSnapshotEventHandler,
        connection: Optional[Connection] = None,
    ) -> None:
        self._event_handler = event_handler
        self._connection: Optional[Connection] = connection
        self._channel: Optional[Channel] = None
        self._queue: Optional[Queue] = None
        self._is_consuming = False

    async def connect(self) -> None:
        if self._connection is None or self._connection.is_closed:
            try:
                self._connection = await aio_pika.connect_robust(settings.RABBITMQ_URL)
                logger.info(
                    "[ClassroomProgressEventConsumer] Connected to RabbitMQ",
                    extra={"url": settings.RABBITMQ_URL.split("@")[-1] if "@" in settings.RABBITMQ_URL else "***"},
                )
            except Exception as e:
                logger.warning(
                    "[ClassroomProgressEventConsumer] Failed to connect to RabbitMQ: %s. Event consumer will be disabled. "
                    "This is OK if RabbitMQ is not available - the service will continue without event processing.",
                    e,
                )
                self._connection = None
                raise

    async def setup_queue(self) -> None:
       
        if self._connection is None or self._connection.is_closed:
            await self.connect()

        # Open channel
        self._channel = await self._connection.channel()

        exchange_name = "EventBus.Messages:ClassroomStudentProgressUpdatedEvent"

        async def _declare_fanout(passive: bool):
            return await self._channel.declare_exchange(
                exchange_name,
                aio_pika.ExchangeType.FANOUT,
                durable=True,
                passive=passive,
            )

        try:
            exchange = await _declare_fanout(passive=True)
        except ChannelNotFoundEntity:
            if self._channel and not self._channel.is_closed:
                await self._channel.close()
            self._channel = await self._connection.channel()
            exchange = await _declare_fanout(passive=False)
        except Exception as e:
            logger.error(
                "[ClassroomProgressEventConsumer] Failed to declare exchange",
                extra={"exchange": exchange_name, "error": str(e)},
                exc_info=True,
            )
            raise

        self._queue = await self._channel.declare_queue(
            settings.RABBITMQ_QUEUE_CLASSROOM_PROGRESS,
            durable=True,
        )

        await self._queue.bind(
            exchange,
            routing_key="",  
        )

        logger.info(
            "[ClassroomProgressEventConsumer] Queue setup complete",
            extra={
                "queue": settings.RABBITMQ_QUEUE_CLASSROOM_PROGRESS,
                "exchange": exchange_name,
                "routing_key": "fanout",
            },
        )

    async def _process_message(self, message: AbstractIncomingMessage) -> None:
        async with message.process():
            try:
                # Parse message body
                body = message.body.decode("utf-8")
                event_data = json.loads(body)

                logger.info(
                    "[ClassroomProgressEventConsumer] Received event",
                    extra={
                        "routing_key": message.routing_key,
                        "student_id": event_data.get("StudentId"),
                        "classroom_id": event_data.get("ClassroomId"),
                    },
                )

                # Transform to ClassroomEvent
                classroom_event = ClassroomEvent(
                    type="STUDENT_PROGRESS_UPDATED",
                    classroom_id=event_data.get("ClassroomId"),
                    student_id=event_data.get("StudentId"),
                    payload={
                        "course_enrollment_id": event_data.get("CourseEnrollmentId"),
                        "course_id": event_data.get("CourseId"),
                        "progress_percentage": event_data.get("ProgressPercentage"),
                        "status": event_data.get("Status"),
                    },
                )

                # Handle event (update snapshot)
                await self._event_handler.handle_event(classroom_event)

                logger.info(
                    "[ClassroomProgressEventConsumer] Successfully processed event",
                    extra={
                        "classroom_id": classroom_event.classroom_id,
                        "student_id": classroom_event.student_id,
                    },
                )

            except json.JSONDecodeError as e:
                logger.error(
                    "[ClassroomProgressEventConsumer] Failed to parse message: %s",
                    e,
                    exc_info=True,
                )
                # Reject message without requeue if parsing fails
                await message.nack(requeue=False)
            except Exception as e:
                logger.error(
                    "[ClassroomProgressEventConsumer] Error processing message: %s",
                    e,
                    exc_info=True,
                )
                # Reject and requeue for retry
                await message.nack(requeue=True)

    async def start_consuming(self) -> None:
        if not settings.ENABLE_EVENT_CONSUMER:
            logger.info(
                "[ClassroomProgressEventConsumer] Event consumer is disabled",
            )
            return

        if self._queue is None:
            await self.setup_queue()

        if self._queue is None:
            raise RuntimeError("Queue not initialized")

        self._is_consuming = True
        logger.info(
            "[ClassroomProgressEventConsumer] Starting to consume messages",
            extra={"queue": settings.RABBITMQ_QUEUE_CLASSROOM_PROGRESS},
        )

        await self._queue.consume(self._process_message)

    async def stop_consuming(self) -> None:
        self._is_consuming = False

        if self._queue:
            await self._queue.cancel()
            logger.info("[ClassroomProgressEventConsumer] Stopped consuming messages")

        if self._channel and not self._channel.is_closed:
            await self._channel.close()

        if self._connection and not self._connection.is_closed:
            await self._connection.close()
            logger.info("[ClassroomProgressEventConsumer] Closed RabbitMQ connection")

    async def __aenter__(self):
        """Async context manager entry."""
        await self.connect()
        await self.setup_queue()
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        """Async context manager exit."""
        await self.stop_consuming()

