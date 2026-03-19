"""
Test script to trigger ClassroomStudentProgressUpdatedEvent
This script publishes a test event to RabbitMQ to test the ingestion pipeline.
"""

import asyncio
import json
import sys
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

import aio_pika
from app.infrastructure.config.settings import settings


async def publish_test_event(
    classroom_id: int,
    student_id: str,
    course_enrollment_id: int = 1,
    course_id: int = 1,
    progress_percentage: int = 50,
    status: str = "InProgress",
):
    """
    Publish a test ClassroomStudentProgressUpdatedEvent to RabbitMQ
    
    Args:
        classroom_id: Classroom ID
        student_id: Student ID (UUID string)
        course_enrollment_id: Course enrollment ID
        course_id: Course ID
        progress_percentage: Progress percentage (0-100)
        status: Status string (e.g., "InProgress", "Completed")
    """
    exchange_name = "EventBus.Messages:ClassroomStudentProgressUpdatedEvent"
    routing_key = "EventBus.Messages:ClassroomStudentProgressUpdatedEvent"
    
    # Create event payload matching C# event structure
    event_data = {
        "StudentId": student_id,
        "ClassroomId": classroom_id,
        "CourseEnrollmentId": course_enrollment_id,
        "CourseId": course_id,
        "ProgressPercentage": progress_percentage,
        "Status": status,
    }
    
    print(f"Connecting to RabbitMQ at {settings.RABBITMQ_URL.split('@')[-1] if '@' in settings.RABBITMQ_URL else '***'}")
    
    try:
        # Connect to RabbitMQ
        connection = await aio_pika.connect_robust(settings.RABBITMQ_URL)
        print("✓ Connected to RabbitMQ")
        
        async with connection:
            channel = await connection.channel()
            
            # Declare exchange
            try:
                exchange = await channel.declare_exchange(
                    exchange_name,
                    aio_pika.ExchangeType.TOPIC,
                    durable=True,
                )
            except Exception:
                exchange = await channel.declare_exchange(
                    exchange_name,
                    aio_pika.ExchangeType.FANOUT,
                    durable=True,
                )
            
            print(f"✓ Exchange '{exchange_name}' declared")
            
            # Publish message
            message_body = json.dumps(event_data).encode("utf-8")
            message = aio_pika.Message(
                body=message_body,
                content_type="application/json",
                delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
            )
            
            await exchange.publish(
                message,
                routing_key=routing_key,
            )
            
            print(f"✓ Event published successfully!")
            print(f"\nEvent details:")
            print(f"  Classroom ID: {classroom_id}")
            print(f"  Student ID: {student_id}")
            print(f"  Course Enrollment ID: {course_enrollment_id}")
            print(f"  Course ID: {course_id}")
            print(f"  Progress: {progress_percentage}%")
            print(f"  Status: {status}")
            print(f"\nThe event should be consumed by ClassroomProgressEventConsumer")
            print(f"and trigger RAG ingestion (with debouncing) for classroom {classroom_id}")
            
    except Exception as e:
        print(f"✗ Error publishing event: {e}")
        import traceback
        traceback.print_exc()
        return False
    
    return True


async def main():
    """Main function with example usage"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Trigger ClassroomStudentProgressUpdatedEvent for testing"
    )
    parser.add_argument(
        "--classroom-id",
        type=int,
        required=True,
        help="Classroom ID"
    )
    parser.add_argument(
        "--student-id",
        type=str,
        required=True,
        help="Student ID (UUID string)"
    )
    parser.add_argument(
        "--course-enrollment-id",
        type=int,
        default=1,
        help="Course enrollment ID (default: 1)"
    )
    parser.add_argument(
        "--course-id",
        type=int,
        default=1,
        help="Course ID (default: 1)"
    )
    parser.add_argument(
        "--progress",
        type=int,
        default=50,
        help="Progress percentage 0-100 (default: 50)"
    )
    parser.add_argument(
        "--status",
        type=str,
        default="InProgress",
        help="Status (default: InProgress)"
    )
    
    args = parser.parse_args()
    
    success = await publish_test_event(
        classroom_id=args.classroom_id,
        student_id=args.student_id,
        course_enrollment_id=args.course_enrollment_id,
        course_id=args.course_id,
        progress_percentage=args.progress,
        status=args.status,
    )
    
    if success:
        print("\n✓ Test event published successfully!")
        print("\nTo verify:")
        print("1. Check logs for '[ClassroomProgressEventConsumer] Received event'")
        print("2. Check logs for '[IngestionService] Scheduled ingestion' (after debounce)")
        print("3. Check logs for '[IngestionService] Starting ingestion' (after 5 minutes)")
    else:
        print("\n✗ Failed to publish test event")
        sys.exit(1)


if __name__ == "__main__":
    asyncio.run(main())



