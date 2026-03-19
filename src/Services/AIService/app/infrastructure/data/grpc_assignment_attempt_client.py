import logging
import sys
from pathlib import Path
from typing import Optional, Dict, Any
import grpc  # type: ignore

from app.infrastructure.data.fixtures.mock_assignment_attempt_data import (
    get_mock_assignment_attempt_data
)

logger = logging.getLogger(__name__)

_CURRENT_DIR = Path(__file__).resolve()
_APP_DIR = _CURRENT_DIR.parent.parent.parent
_GENERATED_DIR = _APP_DIR / "grpc" / "generated"

try:
    # Add generated directory to path for Protos imports
    # Generated code uses "from Protos.Classroom import ..." and "from Protos.Resource import ..."
    _GEN_DIR = Path(__file__).parent.parent.parent / "grpc" / "generated"
    if str(_GEN_DIR) not in sys.path:
        sys.path.insert(0, str(_GEN_DIR))
    
    from Protos.Classroom import assignment_attempt_pb2
    from Protos.Classroom import assignment_attempt_pb2_grpc
except ImportError as e:
    # Fallback if proto files not generated yet
    logger.warning(f"Failed to import assignment_attempt proto files: {e}")
    assignment_attempt_pb2 = None
    assignment_attempt_pb2_grpc = None


class GrpcAssignmentAttemptClient:

    def __init__(
        self,
        endpoint: str,
        use_tls: bool = False,
        cert_path: Optional[str] = None,
        authority_override: Optional[str] = None,
        fallback_mock: Optional[Dict[str, Any]] = None,
    ):
        if assignment_attempt_pb2 is None or assignment_attempt_pb2_grpc is None:
            raise ImportError(
                "AssignmentAttempt proto files not found. Please generate them first."
            )
        
        sanitized_endpoint = endpoint.strip()
        if sanitized_endpoint.startswith(("http://", "https://")):
            sanitized_endpoint = sanitized_endpoint.split("://", 1)[1]
            logger.warning(
                "Removed protocol prefix from gRPC endpoint",
                extra={"original": endpoint, "sanitized": sanitized_endpoint}
            )
        
        self.endpoint = sanitized_endpoint
        self.use_tls = use_tls
        self.cert_path = cert_path
        self.authority_override = authority_override
        self.fallback_mock = fallback_mock
        self._channel: Optional[grpc.aio.Channel] = None
        self._stub: Optional[assignment_attempt_pb2_grpc.GrpcAssignmentAttemptStub] = None
        self._last_call_used_fallback: bool = False

    async def _ensure_channel(self):
        if self._channel is None:
            if self.use_tls:
                credentials = grpc.ssl_channel_credentials()
                if self.cert_path:
                    with open(self.cert_path, "rb") as f:
                        cert_data = f.read()
                    credentials = grpc.ssl_channel_credentials(root_certificates=cert_data)
                
                options = []
                if self.authority_override:
                    options.append(("grpc.ssl_target_name_override", self.authority_override))
                
                self._channel = grpc.aio.secure_channel(self.endpoint, credentials, options=options)
            else:
                self._channel = grpc.aio.insecure_channel(self.endpoint)
            
            self._stub = assignment_attempt_pb2_grpc.GrpcAssignmentAttemptStub(self._channel)
            logger.debug(
                "Created gRPC channel for AssignmentAttempt service",
                extra={"endpoint": self.endpoint, "use_tls": self.use_tls}
            )

    def was_fallback_used(self) -> bool:
        """Check if the last call used fallback mock data."""
        return self._last_call_used_fallback

    async def get_assignment_attempt_by_id(self, attempt_id: int) -> Dict[str, Any]:
        # Reset fallback flag at the start of each call
        self._last_call_used_fallback = False
        
        try:
            await self._ensure_channel()
            
            request = assignment_attempt_pb2.GetAssignmentAttemptByIdRequest(id=attempt_id)
            
            try:
                response = await self._stub.GetAssignmentAttemptById(request)
                
                # Convert proto response to dictionary
                result = {
                    "id": response.id,
                    "studentAssignmentId": response.studentAssignmentId,
                    "teacherId": response.teacherId,
                    "submittedAt": response.submittedAt,
                    "totalScore": response.totalScore,
                    "status": response.status,
                    "feedback": response.feedback,
                    "attemptNumber": response.attemptNumber,
                    "questionAttempts": []
                }
                
                # Map question attempts
                for qa in response.questionAttempts:
                    question_attempt = {
                        "id": qa.id,
                        "assignmentAttemptId": qa.assignmentAttemptId,
                        "assignmentQuestionId": qa.assignmentQuestionId,
                        "answerText": qa.answerText,
                        "answerFileUrl": qa.answerFileUrl,
                        "points": qa.points,
                        "rubricScores": []
                    }
                    
                    # Map rubric scores
                    for rs in qa.rubricScore:
                        rubric_score = {
                            "rubricCriterionId": rs.rubricCriterionId,
                            "currentPoints": rs.currentPoints if rs.HasField("currentPoints") else None,
                            "criterionName": rs.criterionName if rs.HasField("criterionName") else None,
                            "description": rs.description if rs.HasField("description") else None,
                            "maxPoints": rs.maxPoints if rs.HasField("maxPoints") else None,
                        }
                        question_attempt["rubricScores"].append(rubric_score)
                    
                    result["questionAttempts"].append(question_attempt)
                
                logger.info(
                    "Successfully fetched assignment attempt via gRPC",
                    extra={"attempt_id": attempt_id, "question_count": len(result["questionAttempts"])}
                )
                return result
                
            except grpc.RpcError as e:
                logger.warning(
                    "gRPC error fetching assignment attempt, falling back to mock data",
                    extra={"attempt_id": attempt_id, "error": str(e), "code": e.code()}
                )
                # Fall through to fallback
            except Exception as e:
                logger.warning(
                    "Error fetching assignment attempt, falling back to mock data",
                    extra={"attempt_id": attempt_id, "error": str(e)}
                )
                # Fall through to fallback
        
        except Exception as e:
            logger.warning(
                "Failed to establish gRPC connection, using fallback mock data",
                extra={"attempt_id": attempt_id, "error": str(e)}
            )
            # Fall through to fallback
        
        # Use fallback mock data
        if self.fallback_mock:
            logger.info(
                "Using provided fallback mock data for assignment attempt",
                extra={"attempt_id": attempt_id}
            )
            self._last_call_used_fallback = True
            return self.fallback_mock
        
        # Use default mock data
        mock_data = get_mock_assignment_attempt_data(attempt_id)
        question_count = len(mock_data.get("questionAttempts", []))
        logger.info(
            "Using default mock data for assignment attempt",
            extra={
                "attempt_id": attempt_id,
                "questionCount": question_count,
                "questionIds": [qa.get("assignmentQuestionId") for qa in mock_data.get("questionAttempts", [])],
            }
        )
        self._last_call_used_fallback = True
        return mock_data

    async def close(self):
        if self._channel:
            await self._channel.close()
            self._channel = None
            self._stub = None
            logger.debug("Closed gRPC channel for AssignmentAttempt service")

