"""
Lesson repository implementation that fetches data from Resource service via gRPC.
"""

import logging
from pathlib import Path
from typing import List, Optional

import grpc  # type: ignore
from google.protobuf import wrappers_pb2  # type: ignore

from app.core.data.lesson_repository import LessonRepository
from app.core.data.models import LessonDto, LessonSectionDto
from app.infrastructure.data.mock_lesson_repository import MockLessonRepository

from app.grpc.generated.Resource import lesson_pb2
from app.grpc.generated.Resource import lesson_pb2_grpc
from app.grpc.generated.Resource import section_pb2
from app.grpc.generated.Resource import section_pb2_grpc

logger = logging.getLogger(__name__)


class GrpcLessonRepository(LessonRepository):
    """gRPC backed repository for lesson metadata."""

    def __init__(
        self,
        endpoint: str,
        fallback: Optional[MockLessonRepository] = None,
        page_size: int = 50,
        use_tls: bool = False,
        cert_path: Optional[str] = None,
        authority_override: Optional[str] = None,
    ):
        sanitized_endpoint = endpoint.strip()
        if sanitized_endpoint.startswith(("http://", "https://")):
            sanitized_endpoint = sanitized_endpoint.split("://", 1)[1]
            logger.warning(
                "Removed protocol prefix from gRPC endpoint",
                extra={"original": endpoint, "sanitized": sanitized_endpoint}
            )
        if ":" not in sanitized_endpoint:
            logger.warning(
                "gRPC endpoint missing port number. gRPC may use default port.",
                extra={"endpoint": sanitized_endpoint}
            )
        
        self.endpoint = sanitized_endpoint
        self.fallback = fallback
        self.page_size = page_size
        self.use_tls = use_tls
        self.cert_path = cert_path
        self.authority_override = authority_override
        self._channel: Optional[grpc.aio.Channel] = None
        self._lesson_stub: Optional[lesson_pb2_grpc.LessonServiceStub] = None
        self._section_stub: Optional[section_pb2_grpc.SectionServiceStub] = None
        self._ssl_credentials: Optional[grpc.ChannelCredentials] = None
        self._last_call_used_fallback: bool = False

    def was_fallback_used(self) -> bool:
        """Check if the last call to get_lesson_with_sections used fallback data."""
        return self._last_call_used_fallback

    async def get_lesson_with_sections(self, lesson_id: Optional[str] = None) -> LessonDto:
        # Reset fallback flag at the start of each call
        self._last_call_used_fallback = False

        if not lesson_id:
            if self.fallback:
                logger.warning("Lesson ID missing. Falling back to mock repository.")
                self._last_call_used_fallback = True
                return await self.fallback.get_lesson_with_sections()
            raise ValueError("lesson_id is required for gRPC repository")

        logger.debug(
            "Requesting lesson via gRPC",
            extra={"lesson_id": lesson_id, "endpoint": self.endpoint},
        )

        try:
            lesson_id_int = int(lesson_id)
        except (ValueError, TypeError) as e:
            error_msg = f"Invalid lesson_id format: '{lesson_id}'. Expected a numeric value."
            logger.error(error_msg, extra={"lesson_id": lesson_id})
            if self.fallback:
                logger.warning("Falling back to mock repository due to invalid lesson_id.")
                self._last_call_used_fallback = True
                return await self.fallback.get_lesson_with_sections()
            raise ValueError(error_msg) from e

        try:
            lesson_proto = await self._fetch_lesson(lesson_id_int)
            sections = await self._fetch_sections(lesson_proto.id)
            logger.info(
                "Successfully retrieved lesson and sections via gRPC",
                extra={"lesson_id": lesson_id, "section_count": len(sections)},
            )
            return self._map_to_dto(lesson_proto, sections)
        except grpc.aio.AioRpcError as error:
            error_details = {
                "lesson_id": lesson_id,
                "endpoint": self.endpoint,
                "status_code": error.code().name if hasattr(error.code(), 'name') else str(error.code()),
                "status_value": error.code().value[0] if hasattr(error.code(), 'value') else None,
                "details": error.details(),
                "use_tls": self.use_tls,
            }
            logger.exception(
                "gRPC error fetching lesson",
                extra=error_details,
            )
            
            if error.code() == grpc.StatusCode.UNAVAILABLE:
                logger.error(
                    f"gRPC service unavailable. Endpoint: {self.endpoint}. "
                    f"This may indicate: 1) Service is not running, 2) Network connectivity issue, "
                    f"3) Port not accessible, 4) DNS resolution failure.",
                    extra=error_details
                )
            
            if self.fallback:
                logger.warning("Falling back to mock repository due to gRPC error.")
                self._last_call_used_fallback = True
                return await self.fallback.get_lesson_with_sections()
            raise

    async def _fetch_lesson(self, lesson_id: int) -> lesson_pb2.LessonResponse:
        stub = await self._get_lesson_stub()
        return await stub.GetLesson(lesson_pb2.GetLessonRequest(id=lesson_id))

    async def _fetch_sections(self, lesson_id: int) -> List[section_pb2.SectionResponse]:
        stub = await self._get_section_stub()
        sections: List[section_pb2.SectionResponse] = []
        page = 1

        while True:
            logger.debug(
                "Requesting lesson sections page via gRPC",
                extra={"lesson_id": lesson_id, "page": page, "page_size": self.page_size},
            )
            request = section_pb2.QuerySectionsRequest(
                pageNumber=page,
                pageSize=self.page_size,
                lessonId=wrappers_pb2.Int32Value(value=lesson_id),
            )
            response = await stub.QuerySections(request)
            sections.extend(response.items)
            if len(response.items) < self.page_size:
                break
            page += 1

        return sections

    def _map_to_dto(
        self,
        lesson_proto: lesson_pb2.LessonResponse,
        section_protos: List[section_pb2.SectionResponse],
    ) -> LessonDto:
        sections = [
            LessonSectionDto(
                id=str(section.id),
                title=section.title or f"Section {section.orderIndex}",
                description=section.description,
                duration_minutes=section.duration if section.duration > 0 else None,
            )
            for section in section_protos
        ]

        return LessonDto(
            id=str(lesson_proto.id),
            title=lesson_proto.title,
            description=lesson_proto.description,
            learning_outcomes=[lesson_proto.learningOutcome]
            if lesson_proto.learningOutcome
            else [],
            requirements=[lesson_proto.requirement.value]
            if lesson_proto.HasField("requirement")
            else [],
            skills=list(lesson_proto.skillNames),
            topics=list(lesson_proto.topicNames),
            standards=list(lesson_proto.standardNames),
            sections=sections,
        )

    async def _get_lesson_stub(self) -> lesson_pb2_grpc.LessonServiceStub:
        await self._ensure_channel()
        assert self._lesson_stub is not None
        return self._lesson_stub

    async def _get_section_stub(self) -> section_pb2_grpc.SectionServiceStub:
        await self._ensure_channel()
        assert self._section_stub is not None
        return self._section_stub

    async def _ensure_channel(self) -> None:
        if self._channel is None:
            if self.use_tls:
                credentials = self._get_ssl_credentials()
                options = []
                if self.authority_override:
                    options.append(
                        ("grpc.ssl_target_name_override", self.authority_override)
                    )
                self._channel = grpc.aio.secure_channel(
                    self.endpoint,
                    credentials,
                    options=options or None,
                )
                logger.info(
                    "Established secure gRPC channel",
                    extra={"endpoint": self.endpoint, "override": self.authority_override},
                )
            else:
                self._channel = grpc.aio.insecure_channel(self.endpoint)
                logger.info(
                    "Established insecure gRPC channel",
                    extra={"endpoint": self.endpoint},
                )

            self._lesson_stub = lesson_pb2_grpc.LessonServiceStub(self._channel)
            self._section_stub = section_pb2_grpc.SectionServiceStub(self._channel)

    def _get_ssl_credentials(self) -> grpc.ChannelCredentials:
        if self._ssl_credentials is None:
            root_certs = self._load_root_certificates()
            self._ssl_credentials = grpc.ssl_channel_credentials(root_certificates=root_certs)
        return self._ssl_credentials

    def _load_root_certificates(self) -> Optional[bytes]:
        if not self.cert_path:
            logger.debug("No custom gRPC certificate path provided; using system trust store.")
            return None

        cert_file = Path(self.cert_path)
        if not cert_file.exists():
            logger.warning(
                "Provided gRPC certificate path does not exist. Falling back to system trust store.",
                extra={"cert_path": self.cert_path},
            )
            return None

        try:
            data = cert_file.read_bytes()
            logger.info(
                "Loaded custom gRPC root certificate.",
                extra={"cert_path": self.cert_path},
            )
            return data
        except OSError as error:
            logger.warning(
                "Failed to read gRPC certificate file. Falling back to system trust store.",
                extra={"cert_path": self.cert_path, "error": str(error)},
            )
            return None

