"""
Mock Classroom Data for Testing
Complete schema with all required fields for RAG
"""

from datetime import datetime, timedelta
from typing import List, Dict, Any, Tuple
import uuid

# Generate sample UUIDs
STUDENT_ID_1 = str(uuid.uuid4())
STUDENT_ID_2 = str(uuid.uuid4())
STUDENT_ID_3 = str(uuid.uuid4())
TEACHER_ID = str(uuid.uuid4())

# Base dates
BASE_DATE = datetime(2024, 1, 15, 0, 0, 0)
NOW = datetime(2024, 1, 25, 15, 30, 0)

def get_mock_lec_sec_data() -> Dict[str, Any]:
    """
    Generate mock lesson and section data
    """
    return {
         "title": "Urban Planning",
  "publisher": "STEMify",
  "ageRange": "4-7",
  "durationMinutes": 130,
  "description": "Explore the fundamentals of sustainable urban planning—from housing and transportation to energy production, natural-hazard readiness, and eco-friendly city design. Students will prototype a simple solar panel model and integrate it into their own city layouts. By the end of the lesson, learners will extend their designs while aligning with key Sustainable Development Goals (SDGs).",
  "learningOutcomes": [
    "Understand key principles of sustainable urban development including infrastructure, housing, and clean energy.",
    "Apply design-thinking skills by sketching, building, and prototyping components of a solar-powered city."
  ],
  "requirements": [],
  "skills": ["Creativity", "Teamwork"],
  "topics": ["Storytelling"],
  "standards": ["Engineering Design"],
  "sections": [
    {
      "title": "Overview and Objectives",
      "durationMinutes": 10,
      "description": "Introduce the lesson’s theme, goals, and expected outcomes. This segment builds curiosity and sets the stage for learning, connecting the activities to STEM standards and creative problem-solving."
    },
    {
      "title": "Materials",
      "durationMinutes": 40,
      "description": "Walk through all materials needed for hands-on exploration. STEMify Kits and additional tools ensure accessibility for every learner and support a proven, tactile learning approach."
    },
    {
      "title": "Facilitation and Collaboration Work",
      "durationMinutes": 10,
      "description": "Guide students through teamwork-oriented activities. Facilitation prompts help teachers foster collaboration, inquiry, and shared problem-solving to deepen understanding."
    },
    {
      "title": "Preparation",
      "durationMinutes": 10,
      "description": "Teachers prepare for the session using onboarding guidance and expert tips. STEMify Classroom resources support smooth facilitation and build educator confidence."
    },
    {
      "title": "Build",
      "durationMinutes": 30,
      "description": "Students create, test, and refine ideas using STEMify building systems. This hands-on experience encourages inventiveness, resilience, and creative confidence in STEM learning."
    }
  ]
    }

def get_mock_classroom_data_test() -> Dict[str, Any]:
    """
    Generate complete mock classroom data with all required fields
    """
    return {
        "classroom": {
            "id": 1,
            "name": "Lớp 7A - Vật Lý",
            "grade": "7",
            "description": "Lớp học Vật Lý cơ bản cho học sinh lớp 7",
            "teacher_id": TEACHER_ID,
            "curriculum_id": 1,
            "organization_id": 1,
            "organization_subscription_order_id": 1,
            "class_code": "PHY7A2024",
            "cover_image_url": "https://example.com/images/classroom.jpg",
            "status": "Active",
            "start_date": "2024-01-15",
            "end_date": "2024-06-30",
            "created_at": "2024-01-01T00:00:00Z",
            "updated_at": "2024-01-15T00:00:00Z"
        },
        "students": [
            {
                "student_id": STUDENT_ID_1,
                "student_name": "Nguyễn Văn A",
                "email": "nguyenvana@example.com",
                "image_url": "https://example.com/avatars/student1.jpg",
                "joined_at": "2024-01-15T08:00:00Z",
                "classroom_id": 1
            },
            {
                "student_id": STUDENT_ID_2,
                "student_name": "Trần Thị B",
                "email": "tranthib@example.com",
                "image_url": "https://example.com/avatars/student2.jpg",
                "joined_at": "2024-01-15T08:00:00Z",
                "classroom_id": 1
            },
            {
                "student_id": STUDENT_ID_3,
                "student_name": "Lê Văn C",
                "email": "levanc@example.com",
                "image_url": "https://example.com/avatars/student3.jpg",
                "joined_at": "2024-01-16T09:00:00Z",
                "classroom_id": 1
            }
        ],
        "enrollments": {
            "curriculum_enrollments": [
                {
                    "id": 1,
                    "student_id": STUDENT_ID_1,
                    "curriculum_id": 1,
                    "curriculum_name": "Chương trình Vật Lý lớp 7",
                    "classroom_id": 1,
                    "enrolled_at": "2024-01-15T08:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 45,
                    "completed_at": None
                },
                {
                    "id": 2,
                    "student_id": STUDENT_ID_2,
                    "curriculum_id": 1,
                    "curriculum_name": "Chương trình Vật Lý lớp 7",
                    "classroom_id": 1,
                    "enrolled_at": "2024-01-15T08:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 60,
                    "completed_at": None
                },
                {
                    "id": 3,
                    "student_id": STUDENT_ID_3,
                    "curriculum_id": 1,
                    "curriculum_name": "Chương trình Vật Lý lớp 7",
                    "classroom_id": 1,
                    "enrolled_at": "2024-01-16T09:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 30,
                    "completed_at": None
                }
            ],
            "course_enrollments": [
                {
                    "id": 1,
                    "student_id": STUDENT_ID_1,
                    "course_id": 1,
                    "course_name": "Vật Lý Cơ Bản",
                    "curriculum_enrollment_id": 1,
                    "enrolled_at": "2024-01-15T08:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 50,
                    "final_score": None,
                    "completed_at": None
                },
                {
                    "id": 2,
                    "student_id": STUDENT_ID_2,
                    "course_id": 1,
                    "course_name": "Vật Lý Cơ Bản",
                    "curriculum_enrollment_id": 2,
                    "enrolled_at": "2024-01-15T08:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 65,
                    "final_score": None,
                    "completed_at": None
                },
                {
                    "id": 3,
                    "student_id": STUDENT_ID_3,
                    "course_id": 1,
                    "course_name": "Vật Lý Cơ Bản",
                    "curriculum_enrollment_id": 3,
                    "enrolled_at": "2024-01-16T09:00:00Z",
                    "status": "InProgress",
                    "progress_percentage": 35,
                    "final_score": None,
                    "completed_at": None
                }
            ]
        },
        "progress": {
            "lesson_progress": [
                {
                    "id": 1,
                    "enrollment_id": 1,
                    "lesson_id": 10,
                    "lesson_title": "Lực và Chuyển động",
                    "lesson_description": "Bài học về các loại lực và chuyển động",
                    "status": "Completed",
                    "completed_at": "2024-01-20T10:30:00Z"
                },
                {
                    "id": 2,
                    "enrollment_id": 1,
                    "lesson_id": 11,
                    "lesson_title": "Mạch điện",
                    "lesson_description": "Bài học về mạch điện đơn giản",
                    "status": "InProgress",
                    "completed_at": None
                },
                {
                    "id": 3,
                    "enrollment_id": 2,
                    "lesson_id": 10,
                    "lesson_title": "Lực và Chuyển động",
                    "lesson_description": "Bài học về các loại lực và chuyển động",
                    "status": "Completed",
                    "completed_at": "2024-01-19T14:20:00Z"
                },
                {
                    "id": 4,
                    "enrollment_id": 2,
                    "lesson_id": 11,
                    "lesson_title": "Mạch điện",
                    "lesson_description": "Bài học về mạch điện đơn giản",
                    "status": "Completed",
                    "completed_at": "2024-01-22T16:00:00Z"
                },
                {
                    "id": 5,
                    "enrollment_id": 3,
                    "lesson_id": 10,
                    "lesson_title": "Lực và Chuyển động",
                    "lesson_description": "Bài học về các loại lực và chuyển động",
                    "status": "InProgress",
                    "completed_at": None
                }
            ],
            "section_progress": [
                {
                    "id": 1,
                    "student_lesson_progress_id": 1,
                    "section_id": 25,
                    "section_title": "Khái niệm về Lực",
                    "section_description": "Giới thiệu về lực và các loại lực",
                    "status": "Completed",
                    "completed_at": "2024-01-18T14:20:00Z"
                },
                {
                    "id": 2,
                    "student_lesson_progress_id": 1,
                    "section_id": 26,
                    "section_title": "Ứng dụng của Lực",
                    "section_description": "Các ứng dụng thực tế của lực",
                    "status": "Completed",
                    "completed_at": "2024-01-19T10:00:00Z"
                },
                {
                    "id": 3,
                    "student_lesson_progress_id": 2,
                    "section_id": 27,
                    "section_title": "Mạch điện đơn giản",
                    "section_description": "Các thành phần của mạch điện",
                    "status": "InProgress",
                    "completed_at": None
                },
                {
                    "id": 4,
                    "student_lesson_progress_id": 3,
                    "section_id": 25,
                    "section_title": "Khái niệm về Lực",
                    "section_description": "Giới thiệu về lực và các loại lực",
                    "status": "Completed",
                    "completed_at": "2024-01-18T12:00:00Z"
                },
                {
                    "id": 5,
                    "student_lesson_progress_id": 4,
                    "section_id": 27,
                    "section_title": "Mạch điện đơn giản",
                    "section_description": "Các thành phần của mạch điện",
                    "status": "Completed",
                    "completed_at": "2024-01-22T15:30:00Z"
                }
            ]
        },
        "quizzes": {
            "student_quizzes": [
                {
                    "id": 1,
                    "quiz_id": 5,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "student_id": STUDENT_ID_1,
                    "student_section_progress_id": 1,
                    "status": "Passed",
                    "final_score": 85.5,
                    "assigned_at": "2024-01-18T00:00:00Z",
                    "due_date": "2024-01-25T23:59:59Z",
                    "max_attempt_allowed": 3,
                    "attempt_count": 2,
                    "time_limit_minutes": 30
                },
                {
                    "id": 2,
                    "quiz_id": 5,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "student_id": STUDENT_ID_2,
                    "student_section_progress_id": 4,
                    "status": "Passed",
                    "final_score": 92.0,
                    "assigned_at": "2024-01-18T00:00:00Z",
                    "due_date": "2024-01-25T23:59:59Z",
                    "max_attempt_allowed": 3,
                    "attempt_count": 1,
                    "time_limit_minutes": 30
                },
                {
                    "id": 3,
                    "quiz_id": 5,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "student_id": STUDENT_ID_3,
                    "student_section_progress_id": 5,
                    "status": "Failed",
                    "final_score": 55.0,
                    "assigned_at": "2024-01-18T00:00:00Z",
                    "due_date": "2024-01-25T23:59:59Z",
                    "max_attempt_allowed": 3,
                    "attempt_count": 3,
                    "time_limit_minutes": 30
                },
                {
                    "id": 4,
                    "quiz_id": 6,
                    "quiz_title": "Quiz: Mạch điện",
                    "quiz_description": "Kiểm tra kiến thức về mạch điện",
                    "student_id": STUDENT_ID_1,
                    "student_section_progress_id": 3,
                    "status": "Assigned",
                    "final_score": None,
                    "assigned_at": "2024-01-22T00:00:00Z",
                    "due_date": "2024-01-29T23:59:59Z",
                    "max_attempt_allowed": 3,
                    "attempt_count": 0,
                    "time_limit_minutes": 30
                },
                {
                    "id": 5,
                    "quiz_id": 6,
                    "quiz_title": "Quiz: Mạch điện",
                    "quiz_description": "Kiểm tra kiến thức về mạch điện",
                    "student_id": STUDENT_ID_2,
                    "student_section_progress_id": 5,
                    "status": "Passed",
                    "final_score": 88.0,
                    "assigned_at": "2024-01-22T00:00:00Z",
                    "due_date": "2024-01-29T23:59:59Z",
                    "max_attempt_allowed": 3,
                    "attempt_count": 1,
                    "time_limit_minutes": 30
                }
            ],
            "quiz_attempts": [
                {
                    "id": 1,
                    "student_quiz_id": 1,
                    "attempt_number": 1,
                    "status": "Failed",
                    "total_score": 65.0,
                    "started_at": "2024-01-19T09:00:00Z",
                    "completed_at": "2024-01-19T09:25:00Z",
                    "time_spent_minutes": 25,
                    "question_attempts": [
                        {
                            "id": 1,
                            "question_id": 20,
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": True,
                            "score": 10,
                            "answer_attempts": [
                                {
                                    "id": 1,
                                    "answer_id": 50,
                                    "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                                    "is_correct": True,
                                    "is_selected": True
                                },
                                {
                                    "id": 2,
                                    "answer_id": 51,
                                    "answer_content": "Lực là khối lượng của vật",
                                    "is_correct": False,
                                    "is_selected": False
                                }
                            ],
                            "topics": ["Lực", "Chuyển động"]
                        },
                        {
                            "id": 2,
                            "question_id": 21,
                            "question_content": "Đơn vị đo lực là gì?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": False,
                            "score": 0,
                            "answer_attempts": [
                                {
                                    "id": 3,
                                    "answer_id": 52,
                                    "answer_content": "Newton (N)",
                                    "is_correct": True,
                                    "is_selected": False
                                },
                                {
                                    "id": 4,
                                    "answer_id": 53,
                                    "answer_content": "Kilogram (kg)",
                                    "is_correct": False,
                                    "is_selected": True
                                }
                            ],
                            "topics": ["Lực", "Đơn vị đo"]
                        },
                        {
                            "id": 3,
                            "question_id": 22,
                            "question_content": "Khi nào vật chuyển động đều?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": True,
                            "score": 10,
                            "answer_attempts": [
                                {
                                    "id": 5,
                                    "answer_id": 54,
                                    "answer_content": "Khi vận tốc không đổi",
                                    "is_correct": True,
                                    "is_selected": True
                                }
                            ],
                            "topics": ["Chuyển động"]
                        }
                    ]
                },
                {
                    "id": 2,
                    "student_quiz_id": 1,
                    "attempt_number": 2,
                    "status": "Passed",
                    "total_score": 85.5,
                    "started_at": "2024-01-20T10:00:00Z",
                    "completed_at": "2024-01-20T10:22:00Z",
                    "time_spent_minutes": 22,
                    "question_attempts": [
                        {
                            "id": 4,
                            "question_id": 20,
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": True,
                            "score": 10,
                            "answer_attempts": [
                                {
                                    "id": 6,
                                    "answer_id": 50,
                                    "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                                    "is_correct": True,
                                    "is_selected": True
                                }
                            ],
                            "topics": ["Lực", "Chuyển động"]
                        },
                        {
                            "id": 5,
                            "question_id": 21,
                            "question_content": "Đơn vị đo lực là gì?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": True,
                            "score": 10,
                            "answer_attempts": [
                                {
                                    "id": 7,
                                    "answer_id": 52,
                                    "answer_content": "Newton (N)",
                                    "is_correct": True,
                                    "is_selected": True
                                }
                            ],
                            "topics": ["Lực", "Đơn vị đo"]
                        }
                    ]
                },
                {
                    "id": 3,
                    "student_quiz_id": 2,
                    "attempt_number": 1,
                    "status": "Passed",
                    "total_score": 92.0,
                    "started_at": "2024-01-19T14:00:00Z",
                    "completed_at": "2024-01-19T14:18:00Z",
                    "time_spent_minutes": 18,
                    "question_attempts": [
                        {
                            "id": 6,
                            "question_id": 20,
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "question_points": 10,
                            "is_correct": True,
                            "score": 10,
                            "answer_attempts": [
                                {
                                    "id": 8,
                                    "answer_id": 50,
                                    "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                                    "is_correct": True,
                                    "is_selected": True
                                }
                            ],
                            "topics": ["Lực", "Chuyển động"]
                        }
                    ]
                },
                {
                    "id": 4,
                    "student_quiz_id": 3,
                    "attempt_number": 1,
                    "status": "Failed",
                    "total_score": 45.0,
                    "started_at": "2024-01-19T15:00:00Z",
                    "completed_at": "2024-01-19T15:10:00Z",
                    "time_spent_minutes": 10,
                    "question_attempts": []
                },
                {
                    "id": 5,
                    "student_quiz_id": 3,
                    "attempt_number": 2,
                    "status": "Failed",
                    "total_score": 50.0,
                    "started_at": "2024-01-21T10:00:00Z",
                    "completed_at": "2024-01-21T10:15:00Z",
                    "time_spent_minutes": 15,
                    "question_attempts": []
                },
                {
                    "id": 6,
                    "student_quiz_id": 3,
                    "attempt_number": 3,
                    "status": "Failed",
                    "total_score": 55.0,
                    "started_at": "2024-01-23T11:00:00Z",
                    "completed_at": "2024-01-23T11:20:00Z",
                    "time_spent_minutes": 20,
                    "question_attempts": []
                },
                {
                    "id": 7,
                    "student_quiz_id": 5,
                    "attempt_number": 1,
                    "status": "Passed",
                    "total_score": 88.0,
                    "started_at": "2024-01-23T09:00:00Z",
                    "completed_at": "2024-01-23T09:25:00Z",
                    "time_spent_minutes": 25,
                    "question_attempts": []
                }
            ]
        },
        "assignments": {
            "student_assignments": [
                {
                    "id": 1,
                    "assignment_id": 3,
                    "assignment_title": "Bài tập: Thiết kế mạch điện",
                    "assignment_description": "Thiết kế và vẽ sơ đồ mạch điện đơn giản",
                    "student_id": STUDENT_ID_1,
                    "student_section_progress_id": 3,
                    "status": "Passed",
                    "final_score": 88.0,
                    "assigned_at": "2024-01-20T00:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "max_attempt_allowed": 2,
                    "attempt_count": 1
                },
                {
                    "id": 2,
                    "assignment_id": 3,
                    "assignment_title": "Bài tập: Thiết kế mạch điện",
                    "assignment_description": "Thiết kế và vẽ sơ đồ mạch điện đơn giản",
                    "student_id": STUDENT_ID_2,
                    "student_section_progress_id": 5,
                    "status": "Passed",
                    "final_score": 95.0,
                    "assigned_at": "2024-01-20T00:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "max_attempt_allowed": 2,
                    "attempt_count": 1
                },
                {
                    "id": 3,
                    "assignment_id": 3,
                    "assignment_title": "Bài tập: Thiết kế mạch điện",
                    "assignment_description": "Thiết kế và vẽ sơ đồ mạch điện đơn giản",
                    "student_id": STUDENT_ID_3,
                    "student_section_progress_id": 6,
                    "status": "UnderReview",
                    "final_score": None,
                    "assigned_at": "2024-01-20T00:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "max_attempt_allowed": 2,
                    "attempt_count": 1
                }
            ],
            "assignment_attempts": [
                {
                    "id": 1,
                    "student_assignment_id": 1,
                    "attempt_number": 1,
                    "teacher_id": TEACHER_ID,
                    "status": "Passed",
                    "total_score": 88.0,
                    "submitted_at": "2024-01-25T15:30:00Z",
                    "feedback": "Tốt, nhưng cần cải thiện phần giải thích. Sơ đồ mạch điện rõ ràng.",
                    "question_attempts": [
                        {
                            "id": 1,
                            "assignment_question_id": 15,
                            "question_content": "Vẽ sơ đồ mạch điện đơn giản gồm nguồn điện, dây dẫn và bóng đèn",
                            "question_type": "OpenEnded",
                            "question_points": 30,
                            "answer_text": "Sơ đồ gồm nguồn điện (pin), dây dẫn nối từ cực dương qua bóng đèn đến cực âm",
                            "answer_file_url": "https://storage.example.com/assignments/student1_q15.pdf",
                            "points": 28,
                            "rubric_scores": [
                                {
                                    "id": 1,
                                    "rubric_criterion_id": 5,
                                    "criterion_name": "Độ chính xác",
                                    "criterion_description": "Sơ đồ mạch điện chính xác",
                                    "max_points": 15,
                                    "points": 14
                                },
                                {
                                    "id": 2,
                                    "rubric_criterion_id": 6,
                                    "criterion_name": "Giải thích rõ ràng",
                                    "criterion_description": "Giải thích cách mạch hoạt động",
                                    "max_points": 15,
                                    "points": 14
                                }
                            ],
                            "topics": ["Mạch điện", "Điện học"]
                        },
                        {
                            "id": 2,
                            "assignment_question_id": 16,
                            "question_content": "Giải thích tại sao bóng đèn sáng khi đóng mạch",
                            "question_type": "OpenEnded",
                            "question_points": 20,
                            "answer_text": "Khi đóng mạch, dòng điện chạy qua dây dẫn và bóng đèn, làm cho dây tóc nóng lên và phát sáng",
                            "answer_file_url": None,
                            "points": 18,
                            "rubric_scores": [
                                {
                                    "id": 3,
                                    "rubric_criterion_id": 7,
                                    "criterion_name": "Hiểu biết về dòng điện",
                                    "criterion_description": "Giải thích đúng về dòng điện",
                                    "max_points": 10,
                                    "points": 9
                                },
                                {
                                    "id": 4,
                                    "rubric_criterion_id": 8,
                                    "criterion_name": "Giải thích logic",
                                    "criterion_description": "Giải thích có logic và dễ hiểu",
                                    "max_points": 10,
                                    "points": 9
                                }
                            ],
                            "topics": ["Dòng điện", "Mạch điện"]
                        }
                    ]
                },
                {
                    "id": 2,
                    "student_assignment_id": 2,
                    "attempt_number": 1,
                    "teacher_id": TEACHER_ID,
                    "status": "Passed",
                    "total_score": 95.0,
                    "submitted_at": "2024-01-24T16:00:00Z",
                    "feedback": "Xuất sắc! Sơ đồ rõ ràng và giải thích chi tiết.",
                    "question_attempts": [
                        {
                            "id": 3,
                            "assignment_question_id": 15,
                            "question_content": "Vẽ sơ đồ mạch điện đơn giản gồm nguồn điện, dây dẫn và bóng đèn",
                            "question_type": "OpenEnded",
                            "question_points": 30,
                            "answer_text": "Sơ đồ chi tiết với ký hiệu chuẩn...",
                            "answer_file_url": "https://storage.example.com/assignments/student2_q15.pdf",
                            "points": 29,
                            "rubric_scores": [
                                {
                                    "id": 5,
                                    "rubric_criterion_id": 5,
                                    "criterion_name": "Độ chính xác",
                                    "criterion_description": "Sơ đồ mạch điện chính xác",
                                    "max_points": 15,
                                    "points": 15
                                },
                                {
                                    "id": 6,
                                    "rubric_criterion_id": 6,
                                    "criterion_name": "Giải thích rõ ràng",
                                    "criterion_description": "Giải thích cách mạch hoạt động",
                                    "max_points": 15,
                                    "points": 14
                                }
                            ],
                            "topics": ["Mạch điện", "Điện học"]
                        }
                    ]
                },
                {
                    "id": 3,
                    "student_assignment_id": 3,
                    "attempt_number": 1,
                    "teacher_id": TEACHER_ID,
                    "status": "UnderReview",
                    "total_score": 0.0,
                    "submitted_at": "2024-01-26T10:00:00Z",
                    "feedback": None,
                    "question_attempts": [
                        {
                            "id": 4,
                            "assignment_question_id": 15,
                            "question_content": "Vẽ sơ đồ mạch điện đơn giản gồm nguồn điện, dây dẫn và bóng đèn",
                            "question_type": "OpenEnded",
                            "question_points": 30,
                            "answer_text": "Sơ đồ cơ bản...",
                            "answer_file_url": "https://storage.example.com/assignments/student3_q15.pdf",
                            "points": 0,
                            "rubric_scores": [],
                            "topics": ["Mạch điện", "Điện học"]
                        }
                    ]
                }
            ]
        },
        "topics": [
            {
                "topic_id": 1,
                "topic_name": "Lực",
                "lessons": [
                    {
                        "lesson_id": 10,
                        "lesson_title": "Lực và Chuyển động",
                        "sections": [
                            {
                                "section_id": 25,
                                "section_title": "Khái niệm về Lực",
                                "contents": [
                                    {
                                        "content_id": 50,
                                        "content_type": "Quiz",
                                        "content_title": "Quiz: Lực và Chuyển động"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "topic_id": 2,
                "topic_name": "Chuyển động",
                "lessons": [
                    {
                        "lesson_id": 10,
                        "lesson_title": "Lực và Chuyển động",
                        "sections": [
                            {
                                "section_id": 25,
                                "section_title": "Khái niệm về Lực",
                                "contents": [
                                    {
                                        "content_id": 50,
                                        "content_type": "Quiz",
                                        "content_title": "Quiz: Lực và Chuyển động"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "topic_id": 3,
                "topic_name": "Mạch điện",
                "lessons": [
                    {
                        "lesson_id": 11,
                        "lesson_title": "Mạch điện",
                        "sections": [
                            {
                                "section_id": 27,
                                "section_title": "Mạch điện đơn giản",
                                "contents": [
                                    {
                                        "content_id": 51,
                                        "content_type": "Quiz",
                                        "content_title": "Quiz: Mạch điện"
                                    },
                                    {
                                        "content_id": 52,
                                        "content_type": "Assignment",
                                        "content_title": "Bài tập: Thiết kế mạch điện"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "topic_id": 4,
                "topic_name": "Điện học",
                "lessons": [
                    {
                        "lesson_id": 11,
                        "lesson_title": "Mạch điện",
                        "sections": [
                            {
                                "section_id": 27,
                                "section_title": "Mạch điện đơn giản",
                                "contents": [
                                    {
                                        "content_id": 52,
                                        "content_type": "Assignment",
                                        "content_title": "Bài tập: Thiết kế mạch điện"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "topic_id": 5,
                "topic_name": "Dòng điện",
                "lessons": [
                    {
                        "lesson_id": 11,
                        "lesson_title": "Mạch điện",
                        "sections": [
                            {
                                "section_id": 27,
                                "section_title": "Mạch điện đơn giản",
                                "contents": [
                                    {
                                        "content_id": 52,
                                        "content_type": "Assignment",
                                        "content_title": "Bài tập: Thiết kế mạch điện"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "topic_id": 6,
                "topic_name": "Đơn vị đo",
                "lessons": [
                    {
                        "lesson_id": 10,
                        "lesson_title": "Lực và Chuyển động",
                        "sections": [
                            {
                                "section_id": 25,
                                "section_title": "Khái niệm về Lực",
                                "contents": []
                            }
                        ]
                    }
                ]
            }
        ],
        "time_metrics": {
            "quiz_metrics": [
                {
                    "student_id": STUDENT_ID_1,
                    "average_time_per_quiz_minutes": 23.5,
                    "average_time_per_question_minutes": 2.3,
                    "total_quizzes_completed": 2,
                    "total_time_spent_minutes": 47
                },
                {
                    "student_id": STUDENT_ID_2,
                    "average_time_per_quiz_minutes": 21.5,
                    "average_time_per_question_minutes": 2.1,
                    "total_quizzes_completed": 2,
                    "total_time_spent_minutes": 43
                },
                {
                    "student_id": STUDENT_ID_3,
                    "average_time_per_quiz_minutes": 15.0,
                    "average_time_per_question_minutes": 1.5,
                    "total_quizzes_completed": 3,
                    "total_time_spent_minutes": 45
                }
            ],
            "assignment_metrics": [
                {
                    "student_id": STUDENT_ID_1,
                    "average_time_per_assignment_minutes": 120,
                    "total_assignments_completed": 1,
                    "total_time_spent_minutes": 120
                },
                {
                    "student_id": STUDENT_ID_2,
                    "average_time_per_assignment_minutes": 90,
                    "total_assignments_completed": 1,
                    "total_time_spent_minutes": 90
                }
            ],
            "engagement_metrics": [
                {
                    "student_id": STUDENT_ID_1,
                    "last_activity_date": "2024-01-25T15:30:00Z",
                    "days_since_last_activity": 0,
                    "total_activities_last_7_days": 5,
                    "completion_rate": 0.75
                },
                {
                    "student_id": STUDENT_ID_2,
                    "last_activity_date": "2024-01-24T16:00:00Z",
                    "days_since_last_activity": 1,
                    "total_activities_last_7_days": 6,
                    "completion_rate": 1.0
                },
                {
                    "student_id": STUDENT_ID_3,
                    "last_activity_date": "2024-01-23T11:20:00Z",
                    "days_since_last_activity": 2,
                    "total_activities_last_7_days": 3,
                    "completion_rate": 0.5
                }
            ]
        },
        "analysis_period": {
            "from_date": "2024-01-15T00:00:00Z",
            "to_date": "2024-01-25T23:59:59Z",
            "days_back": 7
        }
    }


def get_minimal_classroom_data() -> Dict[str, Any]:
    """
    Generate minimal mock data for quick testing
    """
    return {
        "classroom": {
            "id": 1,
            "name": "Lớp 7A - Vật Lý",
            "grade": "7",
            "teacher_id": TEACHER_ID,
            "curriculum_id": 1,
            "status": "Active"
        },
        "students": [
            {
                "student_id": STUDENT_ID_1,
                "student_name": "Nguyễn Văn A",
                "joined_at": "2024-01-15T00:00:00Z"
            }
        ],
        "quizzes": {
            "student_quizzes": [
                {
                    "id": 1,
                    "quiz_id": 5,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "student_id": STUDENT_ID_1,
                    "status": "Passed",
                    "final_score": 85.5,
                    "attempt_count": 2
                }
            ],
            "quiz_attempts": [
                {
                    "id": 1,
                    "student_quiz_id": 1,
                    "attempt_number": 1,
                    "status": "Failed",
                    "total_score": 65.0,
                    "started_at": "2024-01-19T09:00:00Z",
                    "completed_at": "2024-01-19T09:25:00Z"
                }
            ]
        },
        "analysis_period": {
            "from_date": "2024-01-15T00:00:00Z",
            "to_date": "2024-01-25T23:59:59Z",
            "days_back": 7
        }
    }


def get_topic_focused_data() -> Dict[str, Any]:
    """
    Generate data focused on topic analysis (for testing weak topics detection)
    """
    data = get_mock_classroom_data_test()
    
    # Add more quiz attempts with different topics
    data["quizzes"]["quiz_attempts"].extend([
        {
            "id": 8,
            "student_quiz_id": 1,
            "attempt_number": 3,
            "status": "Passed",
            "total_score": 90.0,
            "started_at": "2024-01-21T10:00:00Z",
            "completed_at": "2024-01-21T10:20:00Z",
            "time_spent_minutes": 20,
            "question_attempts": [
                {
                    "id": 7,
                    "question_id": 23,
                    "question_content": "Tính lực tác dụng lên vật",
                    "question_type": "Calculation",
                    "question_points": 15,
                    "is_correct": False,
                    "score": 0,
                    "answer_attempts": [],
                    "topics": ["Lực", "Tính toán"]
                }
            ]
        }
    ])
    
    return data


def validate_schema(data: Dict[str, Any]) -> Tuple[bool, List[str]]:
    """
    Validate that data matches expected schema
    Returns: (is_valid, list_of_errors)
    """
    errors = []
    required_top_level_keys = [
        "classroom", "students", "enrollments", "progress",
        "quizzes", "assignments", "topics", "time_metrics", "analysis_period"
    ]
    
    # Check top-level keys
    for key in required_top_level_keys:
        if key not in data:
            errors.append(f"Missing top-level key: {key}")
    
    # Validate classroom
    if "classroom" in data:
        classroom = data["classroom"]
        required_classroom_fields = ["id", "name", "grade", "teacher_id", "curriculum_id", "status"]
        for field in required_classroom_fields:
            if field not in classroom:
                errors.append(f"Missing classroom field: {field}")
    
    # Validate students
    if "students" in data:
        if not isinstance(data["students"], list):
            errors.append("students must be a list")
        else:
            for i, student in enumerate(data["students"]):
                required_student_fields = ["student_id", "student_name", "joined_at"]
                for field in required_student_fields:
                    if field not in student:
                        errors.append(f"Student {i} missing field: {field}")
    
    # Validate quizzes structure
    if "quizzes" in data:
        quizzes = data["quizzes"]
        if "student_quizzes" not in quizzes:
            errors.append("quizzes.student_quizzes is required")
        if "quiz_attempts" not in quizzes:
            errors.append("quizzes.quiz_attempts is required")
        
        # Validate quiz attempts
        if "quiz_attempts" in quizzes:
            for i, attempt in enumerate(quizzes["quiz_attempts"]):
                required_attempt_fields = ["id", "student_quiz_id", "attempt_number", "status", "total_score", "started_at"]
                for field in required_attempt_fields:
                    if field not in attempt:
                        errors.append(f"Quiz attempt {i} missing field: {field}")
                
                # Validate question attempts if present
                if "question_attempts" in attempt:
                    for j, qa in enumerate(attempt["question_attempts"]):
                        required_qa_fields = ["id", "question_id", "question_content", "is_correct", "score"]
                        for field in required_qa_fields:
                            if field not in qa:
                                errors.append(f"Quiz attempt {i}, question {j} missing field: {field}")
    
    # Validate assignments structure
    if "assignments" in data:
        assignments = data["assignments"]
        if "student_assignments" not in assignments:
            errors.append("assignments.student_assignments is required")
        if "assignment_attempts" not in assignments:
            errors.append("assignments.assignment_attempts is required")
    
    # Validate topics
    if "topics" in data:
        if not isinstance(data["topics"], list):
            errors.append("topics must be a list")
        else:
            for i, topic in enumerate(data["topics"]):
                if "topic_id" not in topic or "topic_name" not in topic:
                    errors.append(f"Topic {i} missing topic_id or topic_name")
    
    # Validate analysis period
    if "analysis_period" in data:
        period = data["analysis_period"]
        required_period_fields = ["from_date", "to_date", "days_back"]
        for field in required_period_fields:
            if field not in period:
                errors.append(f"analysis_period missing field: {field}")
    
    return len(errors) == 0, errors

