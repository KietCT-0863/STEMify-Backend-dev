from typing import Dict, Any


def get_mock_assignment_attempt_data(attempt_id: int) -> Dict[str, Any]:
    return {
        "id": attempt_id,
        "studentAssignmentId": 100,
        "teacherId": "teacher_123",
        "submittedAt": "2024-12-20T10:30:00Z",
        "totalScore": 0.0,
        "status": "UnderReview",
        "feedback": "",
        "attemptNumber": 1,
        "questionAttempts": [
            {
                "id": 1,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 1,
                "answerText": "Đây là câu trả lời bằng text của học sinh cho câu hỏi 1. Học sinh đã giải thích chi tiết về cách giải bài toán.",
                "answerFileUrl": "",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 2,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 2,
                "answerText": "",
                "answerFileUrl": "https://example.com/files/submission_2.pdf",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 3,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 3,
                "answerText": "Câu trả lời ngắn gọn cho câu hỏi 3.",
                "answerFileUrl": "https://example.com/files/diagram_3.png",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 4,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 4,
                "answerText": "",
                "answerFileUrl": "https://example.com/files/code_solution_4.py",
                "points": 0.0,
                "rubricScores": []
            }
        ]
    }


def get_mock_assignment_attempt_with_images(attempt_id: int) -> Dict[str, Any]:
    """
    Generate mock assignment attempt data with image submissions.
    """
    return {
        "id": attempt_id,
        "studentAssignmentId": 100,
        "teacherId": "teacher_123",
        "submittedAt": "2024-12-20T10:30:00Z",
        "totalScore": 0.0,
        "status": "UnderReview",
        "feedback": "",
        "attemptNumber": 1,
        "questionAttempts": [
            {
                "id": 1,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 1,
                "answerText": "",
                "answerFileUrl": "https://example.com/files/math_work_1.jpg",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 2,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 2,
                "answerText": "",
                "answerFileUrl": "https://example.com/files/science_diagram_2.png",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 3,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 3,
                "answerText": "",
                "answerFileUrl": "https://example.com/files/essay_3.pdf",
                "points": 0.0,
                "rubricScores": []
            }
        ]
    }


def get_mock_assignment_attempt_mixed(attempt_id: int) -> Dict[str, Any]:
    """
    Generate mock assignment attempt data with mixed text and file submissions.
    """
    return {
        "id": attempt_id,
        "studentAssignmentId": 100,
        "teacherId": "teacher_123",
        "submittedAt": "2024-12-20T10:30:00Z",
        "totalScore": 0.0,
        "status": "UnderReview",
        "feedback": "",
        "attemptNumber": 1,
        "questionAttempts": [
            {
                "id": 1,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 1,
                "answerText": "Tôi đã giải bài toán này bằng cách sử dụng công thức Pythagoras. Kết quả là 5cm.",
                "answerFileUrl": "",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 2,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 2,
                "answerText": "",
                "answerFileUrl": "https://example.com/storage/assignments/student_work_2.jpg",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 3,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 3,
                "answerText": "Giải thích: Tôi đã vẽ sơ đồ mạch điện như trong file đính kèm.",
                "answerFileUrl": "https://example.com/storage/assignments/circuit_diagram_3.png",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 4,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 4,
                "answerText": "",
                "answerFileUrl": "https://example.com/storage/assignments/program_code_4.py",
                "points": 0.0,
                "rubricScores": []
            },
            {
                "id": 5,
                "assignmentAttemptId": attempt_id,
                "assignmentQuestionId": 5,
                "answerText": "Đây là bài luận về chủ đề khoa học. Tôi đã nghiên cứu và viết về tác động của biến đổi khí hậu đến hệ sinh thái biển.",
                "answerFileUrl": "https://example.com/storage/assignments/essay_5.docx",
                "points": 0.0,
                "rubricScores": []
            }
        ]
    }

