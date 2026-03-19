from typing import Dict, Any, Optional
import logging
import json
import asyncio
import base64

from app.core.tools.base import Tool
from app.core.tools.file_helper import download_file_bytes, detect_file_type_from_url, is_url

logger = logging.getLogger(__name__)


class SubmissionTool(Tool):

    def __init__(self, assignment_attempt_data: Dict[str, Any]):

        super().__init__(
            name="submission",
            description="Fetch student submission content and metadata for grading. Supports text and file submissions (images, PDFs, documents).",
        )
        self.assignment_attempt_data = assignment_attempt_data

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Get submission content for a specific question attempt.
        
        Expected parameters:
        - assignmentQuestionId: int (optional, if not provided returns all questions)
        """
        question_id = parameters.get("assignmentQuestionId")
        
        all_question_attempts = self.assignment_attempt_data.get("questionAttempts", [])
        total_questions = len(all_question_attempts)
        
        logger.info(
            "[SubmissionTool] Processing submission",
            extra={
                "assignmentAttemptId": self.assignment_attempt_data.get("id"),
                "totalQuestionsInData": total_questions,
                "requestedQuestionId": question_id,
            }
        )
        
        question_attempts = all_question_attempts
        
        if question_id is not None:
            # Filter by specific question
            question_attempts = [
                qa for qa in question_attempts 
                if qa.get("assignmentQuestionId") == question_id
            ]
            logger.info(
                "[SubmissionTool] Filtered to specific question",
                extra={
                    "requestedQuestionId": question_id,
                    "filteredCount": len(question_attempts),
                }
            )
        
        if not question_attempts:
            logger.warning(
                "[SubmissionTool] No question attempts found",
                extra={
                    "requestedQuestionId": question_id,
                    "totalQuestionsInData": total_questions,
                }
            )
            return json.dumps({
                "error": f"No question attempts found" + (f" for question {question_id}" if question_id else ""),
                "questionAttempts": []
            })
        
        question_data_list = []
        download_tasks = []
        
        for qa in question_attempts:
            attempt_data = {
                "id": qa.get("id"),
                "assignmentQuestionId": qa.get("assignmentQuestionId"),
                "answerText": qa.get("answerText"),
                "answerFileUrl": qa.get("answerFileUrl"),
                "points": qa.get("points"),
                "fileType": None,
                "fileContent": None,
            }
            
            # Handle file submission 
            answer_file_url = qa.get("answerFileUrl")
            if answer_file_url and answer_file_url.strip():
                # Detect file type from URL
                file_type = detect_file_type_from_url(answer_file_url)
                attempt_data["fileType"] = file_type
                
                if is_url(answer_file_url):
                    download_tasks.append((qa.get("assignmentQuestionId"), answer_file_url, file_type, attempt_data))
                else:
                    # Local file path (not a URL)
                    attempt_data["fileContent"] = {
                        "filePath": answer_file_url,
                        "fileType": file_type,
                    }
            
            question_data_list.append(attempt_data)
        
        if download_tasks:
            async def download_single_file(question_id: int, url: str, file_type: Optional[str], attempt_data: Dict[str, Any]) -> None:
                try:
                    file_bytes = await download_file_bytes(url)
                    if file_bytes:
                        attempt_data["fileContent"] = {
                            "base64": base64.b64encode(file_bytes).decode("utf-8"),
                            "sizeBytes": len(file_bytes),
                            "fileType": file_type,
                        }
                        logger.debug(
                            "Downloaded file for question attempt",
                            extra={
                                "questionId": question_id,
                                "fileType": file_type,
                                "sizeBytes": len(file_bytes)
                            }
                        )
                    else:
                        logger.warning(
                            "Failed to download file for question attempt",
                            extra={"questionId": question_id, "url": url}
                        )
                except Exception as e:
                    logger.error(
                        "Error downloading file for question attempt",
                        extra={"questionId": question_id, "url": url, "error": str(e)},
                        exc_info=True
                    )
            
            download_coroutines = [
                download_single_file(qid, url, ftype, adata)
                for qid, url, ftype, adata in download_tasks
            ]
            await asyncio.gather(*download_coroutines, return_exceptions=True)
        
        processed_attempts = question_data_list
        
        logger.info(
            "[SubmissionTool] Successfully processed question attempts",
            extra={
                "assignmentAttemptId": self.assignment_attempt_data.get("id"),
                "totalQuestionsInData": total_questions,
                "processedCount": len(processed_attempts),
                "questionIds": [qa.get("assignmentQuestionId") for qa in processed_attempts],
            }
        )
        
        return json.dumps({
            "assignmentAttemptId": self.assignment_attempt_data.get("id"),
            "studentAssignmentId": self.assignment_attempt_data.get("studentAssignmentId"),
            "submittedAt": self.assignment_attempt_data.get("submittedAt"),
            "status": self.assignment_attempt_data.get("status"),
            "attemptNumber": self.assignment_attempt_data.get("attemptNumber"),
            "questionAttempts": processed_attempts,
        })

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "assignmentQuestionId": {
                    "type": "integer",
                    "description": "Optional: Specific assignment question ID to get submission for. If not provided, returns all questions.",
                },
            },
            "required": [],
        }


