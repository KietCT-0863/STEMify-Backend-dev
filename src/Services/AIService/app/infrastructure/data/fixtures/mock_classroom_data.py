from typing import Dict, Any
import uuid


# Generate sample UUIDs
STUDENT_ID_1 = str(uuid.uuid4())
STUDENT_ID_2 = str(uuid.uuid4())
STUDENT_ID_3 = str(uuid.uuid4())
STUDENT_ID_4 = str(uuid.uuid4())
STUDENT_ID_5 = str(uuid.uuid4())
STUDENT_ID_6 = str(uuid.uuid4())
STUDENT_ID_7 = str(uuid.uuid4())
STUDENT_ID_8 = str(uuid.uuid4())
STUDENT_ID_9 = str(uuid.uuid4())
STUDENT_ID_10 = str(uuid.uuid4())
STUDENT_ID_11 = str(uuid.uuid4())
STUDENT_ID_12 = str(uuid.uuid4())
STUDENT_ID_13 = str(uuid.uuid4())
STUDENT_ID_14 = str(uuid.uuid4())
STUDENT_ID_15 = str(uuid.uuid4())
STUDENT_ID_16 = str(uuid.uuid4())
STUDENT_ID_17 = str(uuid.uuid4())
STUDENT_ID_18 = str(uuid.uuid4())
STUDENT_ID_19 = str(uuid.uuid4())
STUDENT_ID_20 = str(uuid.uuid4())


def get_mock_classroom_data() -> Dict[str, Any]:
    """
    Generate optimized mock classroom data with only required fields for student analysis.
    Based on DATA_ANALYSIS.md requirements.
    """
    return {
        "classroom": {
            "id": 1,
            "name": "Lớp 7A - Vật Lý"
        },
        "students": [
            {
                "student_id": STUDENT_ID_1,
                "student_name": "Nguyễn Văn A",
                "joined_at": "2024-01-15T08:00:00Z"
            },
            {
                "student_id": STUDENT_ID_2,
                "student_name": "Trần Thị B",
                "joined_at": "2024-01-15T08:00:00Z"
            },
            {
                "student_id": STUDENT_ID_3,
                "student_name": "Lê Văn C",
                "joined_at": "2024-01-16T09:00:00Z"
            },
            {
                "student_id": STUDENT_ID_4,
                "student_name": "Phạm Thị D",
                "joined_at": "2024-01-16T09:30:00Z"
            },
            {
                "student_id": STUDENT_ID_5,
                "student_name": "Vũ Văn E",
                "joined_at": "2024-01-16T10:00:00Z"
            },
            {
                "student_id": STUDENT_ID_6,
                "student_name": "Đỗ Thị F",
                "joined_at": "2024-01-16T10:30:00Z"
            },
            {
                "student_id": STUDENT_ID_7,
                "student_name": "Hoàng Văn G",
                "joined_at": "2024-01-16T11:00:00Z"
            },
            {
                "student_id": STUDENT_ID_8,
                "student_name": "Bùi Thị H",
                "joined_at": "2024-01-16T11:30:00Z"
            },
            {
                "student_id": STUDENT_ID_9,
                "student_name": "Ngô Văn I",
                "joined_at": "2024-01-16T12:00:00Z"
            },
            {
                "student_id": STUDENT_ID_10,
                "student_name": "Lý Thị K",
                "joined_at": "2024-01-16T12:30:00Z"
            },
            {
                "student_id": STUDENT_ID_11,
                "student_name": "Trịnh Văn L",
                "joined_at": "2024-01-16T13:00:00Z"
            },
            {
                "student_id": STUDENT_ID_12,
                "student_name": "Phan Thị M",
                "joined_at": "2024-01-16T13:30:00Z"
            },
            {
                "student_id": STUDENT_ID_13,
                "student_name": "Tô Văn N",
                "joined_at": "2024-01-16T14:00:00Z"
            },
            {
                "student_id": STUDENT_ID_14,
                "student_name": "Mai Thị O",
                "joined_at": "2024-01-16T14:30:00Z"
            },
            {
                "student_id": STUDENT_ID_15,
                "student_name": "Đặng Văn P",
                "joined_at": "2024-01-16T15:00:00Z"
            },
            {
                "student_id": STUDENT_ID_16,
                "student_name": "Lâm Thị Q",
                "joined_at": "2024-01-16T15:30:00Z"
            },
            {
                "student_id": STUDENT_ID_17,
                "student_name": "Đinh Văn R",
                "joined_at": "2024-01-16T16:00:00Z"
            },
            {
                "student_id": STUDENT_ID_18,
                "student_name": "Cao Thị S",
                "joined_at": "2024-01-16T16:30:00Z"
            },
            {
                "student_id": STUDENT_ID_19,
                "student_name": "Chu Văn T",
                "joined_at": "2024-01-16T17:00:00Z"
            },
            {
                "student_id": STUDENT_ID_20,
                "student_name": "La Thị U",
                "joined_at": "2024-01-16T17:30:00Z"
            }
        ],
        "enrollments": {
            "curriculum_enrollments": [
                {
                    "student_id": STUDENT_ID_1,
                    "progress_percentage": 45,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_2,
                    "progress_percentage": 60,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_3,
                    "progress_percentage": 30,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_4,
                    "progress_percentage": 55,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_5,
                    "progress_percentage": 70,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_6,
                    "progress_percentage": 40,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_7,
                    "progress_percentage": 65,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_8,
                    "progress_percentage": 50,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_9,
                    "progress_percentage": 35,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                },
                {
                    "student_id": STUDENT_ID_10,
                    "progress_percentage": 75,
                    "curriculum_name": "Chương trình Vật Lý lớp 7"
                }
            ],
            "course_enrollments": [
                {
                    "student_id": STUDENT_ID_1,
                    "progress_percentage": 50
                },
                {
                    "student_id": STUDENT_ID_2,
                    "progress_percentage": 65
                },
                {
                    "student_id": STUDENT_ID_3,
                    "progress_percentage": 35
                },
                {
                    "student_id": STUDENT_ID_4,
                    "progress_percentage": 58
                },
                {
                    "student_id": STUDENT_ID_5,
                    "progress_percentage": 72
                },
                {
                    "student_id": STUDENT_ID_6,
                    "progress_percentage": 42
                },
                {
                    "student_id": STUDENT_ID_7,
                    "progress_percentage": 68
                },
                {
                    "student_id": STUDENT_ID_8,
                    "progress_percentage": 52
                },
                {
                    "student_id": STUDENT_ID_9,
                    "progress_percentage": 38
                },
                {
                    "student_id": STUDENT_ID_10,
                    "progress_percentage": 78
                }
            ]
        },
        "quizzes": {
            "student_quizzes": [
                {
                    "id": 1,
                    "student_id": STUDENT_ID_1,
                    "final_score": 85.5,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "attempt_count": 2
                },
                {
                    "id": 2,
                    "student_id": STUDENT_ID_2,
                    "final_score": 92.0,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "attempt_count": 1
                },
                {
                    "id": 3,
                    "student_id": STUDENT_ID_3,
                    "final_score": 55.0,
                    "quiz_title": "Quiz: Lực và Chuyển động",
                    "quiz_description": "Kiểm tra kiến thức về lực và chuyển động",
                    "attempt_count": 3
                },
                {
                    "id": 4,
                    "student_id": STUDENT_ID_1,
                    "final_score": None,
                    "quiz_title": "Quiz: Mạch điện",
                    "quiz_description": "Kiểm tra kiến thức về mạch điện",
                    "attempt_count": 0
                },
                {
                    "id": 5,
                    "student_id": STUDENT_ID_2,
                    "final_score": 88.0,
                    "quiz_title": "Quiz: Mạch điện",
                    "quiz_description": "Kiểm tra kiến thức về mạch điện",
                    "attempt_count": 1
                }
            ],
            "quiz_attempts": [
                {
                    "student_quiz_id": 1,
                    "attempt_number": 1,
                    "time_spent_minutes": 25.0,
                    "total_score": 65.0,
                    "status": "Failed",
                    "question_attempts": [
                        {
                            "is_correct": True,
                            "topics": ["Lực", "Chuyển động"],
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                            "is_selected": True
                        },
                        {
                            "is_correct": False,
                            "topics": ["Lực", "Đơn vị đo"],
                            "question_content": "Đơn vị đo lực là gì?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Kilogram (kg)",
                            "is_selected": True
                        },
                        {
                            "is_correct": True,
                            "topics": ["Chuyển động"],
                            "question_content": "Khi nào vật chuyển động đều?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Khi vận tốc không đổi",
                            "is_selected": True
                        }
                    ]
                },
                {
                    "student_quiz_id": 1,
                    "attempt_number": 2,
                    "time_spent_minutes": 22.0,
                    "total_score": 85.5,
                    "status": "Passed",
                    "question_attempts": [
                        {
                            "is_correct": True,
                            "topics": ["Lực", "Chuyển động"],
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                            "is_selected": True
                        },
                        {
                            "is_correct": True,
                            "topics": ["Lực", "Đơn vị đo"],
                            "question_content": "Đơn vị đo lực là gì?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Newton (N)",
                            "is_selected": True
                        }
                    ]
                },
                {
                    "student_quiz_id": 2,
                    "attempt_number": 1,
                    "time_spent_minutes": 18.0,
                    "total_score": 92.0,
                    "status": "Passed",
                    "question_attempts": [
                        {
                            "is_correct": True,
                            "topics": ["Lực", "Chuyển động"],
                            "question_content": "Lực là gì?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Lực là tác dụng làm thay đổi chuyển động",
                            "is_selected": True
                        }
                    ]
                },
                {
                    "student_quiz_id": 3,
                    "attempt_number": 1,
                    "time_spent_minutes": 10.0,
                    "total_score": 45.0,
                    "status": "Failed",
                    "question_attempts": []
                },
                {
                    "student_quiz_id": 3,
                    "attempt_number": 2,
                    "time_spent_minutes": 15.0,
                    "total_score": 50.0,
                    "status": "Failed",
                    "question_attempts": []
                },
                {
                    "student_quiz_id": 3,
                    "attempt_number": 3,
                    "time_spent_minutes": 20.0,
                    "total_score": 55.0,
                    "status": "Failed",
                    "question_attempts": []
                },
                {
                    "student_quiz_id": 5,
                    "attempt_number": 1,
                    "time_spent_minutes": 25.0,
                    "total_score": 88.0,
                    "status": "Passed",
                    "question_attempts": [
                        {
                            "is_correct": True,
                            "topics": ["Mạch điện", "Điện học"],
                            "question_content": "Các thành phần của mạch điện?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Nguồn điện, dây dẫn, bóng đèn",
                            "is_selected": True
                        },
                        {
                            "is_correct": True,
                            "topics": ["Dòng điện", "Mạch điện"],
                            "question_content": "Dòng điện chạy như thế nào?",
                            "question_type": "MultipleChoice",
                            "answer_content": "Từ cực dương sang cực âm",
                            "is_selected": True
                        }
                    ]
                }
            ]
        },
        "assignments": {
            "student_assignments": [
                {
                    "student_id": STUDENT_ID_1,
                    "final_score": 88.0,
                    "submission_count": 1,
                    "submitted_at": "2024-01-25T15:30:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "question_attempts": [
                        {
                            "answer_text": "Sơ đồ gồm nguồn điện (pin), dây dẫn nối từ cực dương qua bóng đèn đến cực âm",
                            "points": 28,
                            "feedback": "Tốt, nhưng cần cải thiện phần giải thích. Sơ đồ mạch điện rõ ràng.",
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
                            ]
                        }
                    ]
                },
                {
                    "student_id": STUDENT_ID_2,
                    "final_score": 95.0,
                    "submission_count": 1,
                    "submitted_at": "2024-01-24T16:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "question_attempts": [
                        {
                            "answer_text": "Sơ đồ chi tiết với ký hiệu chuẩn...",
                            "points": 29,
                            "feedback": "Xuất sắc! Sơ đồ rõ ràng và giải thích chi tiết.",
                            "rubric_scores": [
                                {
                                    "id": 3,
                                    "rubric_criterion_id": 5,
                                    "criterion_name": "Độ chính xác",
                                    "criterion_description": "Sơ đồ mạch điện chính xác",
                                    "max_points": 15,
                                    "points": 15
                                },
                                {
                                    "id": 4,
                                    "rubric_criterion_id": 6,
                                    "criterion_name": "Giải thích rõ ràng",
                                    "criterion_description": "Giải thích cách mạch hoạt động",
                                    "max_points": 15,
                                    "points": 14
                                }
                            ]
                        }
                    ]
                },
                {
                    "student_id": STUDENT_ID_3,
                    "final_score": None,
                    "submission_count": 1,
                    "submitted_at": "2024-01-26T10:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "question_attempts": [
                        {
                            "answer_text": "Sơ đồ cơ bản...",
                            "points": 0,
                            "feedback": None,
                            "rubric_scores": []
                        }
                    ]
                },
                {
                    "student_id": STUDENT_ID_4,
                    "final_score": 75.0,
                    "submission_count": 2,
                    "submitted_at": "2024-01-28T12:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "question_attempts": []
                },
                {
                    "student_id": STUDENT_ID_5,
                    "final_score": 90.0,
                    "submission_count": 1,
                    "submitted_at": "2024-01-26T14:00:00Z",
                    "due_date": "2024-01-27T23:59:59Z",
                    "question_attempts": []
                }
            ]
        },
        "time_metrics": {
            "engagement_metrics": [
                {
                    "student_id": STUDENT_ID_1,
                    "completion_rate": 0.75,
                    "days_since_last_activity": 0,
                    "active_days_last_7_days": 5,
                    "avg_session_duration_minutes": 25.0
                },
                {
                    "student_id": STUDENT_ID_2,
                    "completion_rate": 1.0,
                    "days_since_last_activity": 1,
                    "active_days_last_7_days": 6,
                    "avg_session_duration_minutes": 30.0
                },
                {
                    "student_id": STUDENT_ID_3,
                    "completion_rate": 0.5,
                    "days_since_last_activity": 2,
                    "active_days_last_7_days": 3,
                    "avg_session_duration_minutes": 15.0
                },
                {
                    "student_id": STUDENT_ID_4,
                    "completion_rate": 0.65,
                    "days_since_last_activity": 1,
                    "active_days_last_7_days": 4,
                    "avg_session_duration_minutes": 20.0
                },
                {
                    "student_id": STUDENT_ID_5,
                    "completion_rate": 0.85,
                    "days_since_last_activity": 0,
                    "active_days_last_7_days": 5,
                    "avg_session_duration_minutes": 28.0
                },
                {
                    "student_id": STUDENT_ID_6,
                    "completion_rate": 0.45,
                    "days_since_last_activity": 3,
                    "active_days_last_7_days": 2,
                    "avg_session_duration_minutes": 12.0
                },
                {
                    "student_id": STUDENT_ID_7,
                    "completion_rate": 0.80,
                    "days_since_last_activity": 1,
                    "active_days_last_7_days": 5,
                    "avg_session_duration_minutes": 22.0
                },
                {
                    "student_id": STUDENT_ID_8,
                    "completion_rate": 0.60,
                    "days_since_last_activity": 2,
                    "active_days_last_7_days": 4,
                    "avg_session_duration_minutes": 18.0
                },
                {
                    "student_id": STUDENT_ID_9,
                    "completion_rate": 0.40,
                    "days_since_last_activity": 4,
                    "active_days_last_7_days": 2,
                    "avg_session_duration_minutes": 10.0
                },
                {
                    "student_id": STUDENT_ID_10,
                    "completion_rate": 0.90,
                    "days_since_last_activity": 0,
                    "active_days_last_7_days": 6,
                    "avg_session_duration_minutes": 35.0
                }
            ]
        },
        "progress": {
            "section_progress": [
                {
                    "student_id": STUDENT_ID_1,
                    "section_id": 25,
                    "section_name": "Khái niệm về Lực",
                    "status": "Completed",
                    "last_activity_at": "2024-01-20T10:30:00Z"
                },
                {
                    "student_id": STUDENT_ID_1,
                    "section_id": 27,
                    "section_name": "Mạch điện đơn giản",
                    "status": "InProgress",
                    "last_activity_at": "2024-01-22T15:00:00Z"
                },
                {
                    "student_id": STUDENT_ID_2,
                    "section_id": 25,
                    "section_name": "Khái niệm về Lực",
                    "status": "Completed",
                    "last_activity_at": "2024-01-19T14:20:00Z"
                },
                {
                    "student_id": STUDENT_ID_2,
                    "section_id": 27,
                    "section_name": "Mạch điện đơn giản",
                    "status": "Completed",
                    "last_activity_at": "2024-01-22T16:00:00Z"
                },
                {
                    "student_id": STUDENT_ID_3,
                    "section_id": 25,
                    "section_name": "Khái niệm về Lực",
                    "status": "InProgress",
                    "last_activity_at": "2024-01-19T15:00:00Z"
                }
            ]
        },
        "topics_catalog": [
            {
                "topic_id": 1,
                "topic_name": "Lực",
                "parent_topic_id": None,
                "sections": [
                    {
                        "section_title": "Khái niệm về Lực",
                        "contents": [
                            {
                                "content_type": "Quiz",
                                "content_title": "Quiz: Lực và Chuyển động"
                            }
                        ]
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Lực và Chuyển động",
                        "lesson_description": "Bài học về các loại lực và chuyển động"
                    }
                ]
            },
            {
                "topic_id": 2,
                "topic_name": "Newton's Second Law",
                "parent_topic_id": 1,
                "sections": [],
                "lessons": []
            },
            {
                "topic_id": 3,
                "topic_name": "Chuyển động",
                "parent_topic_id": None,
                "sections": [
                    {
                        "section_title": "Khái niệm về Lực",
                        "contents": [
                            {
                                "content_type": "Quiz",
                                "content_title": "Quiz: Lực và Chuyển động"
                            }
                        ]
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Lực và Chuyển động",
                        "lesson_description": "Bài học về các loại lực và chuyển động"
                    }
                ]
            },
            {
                "topic_id": 4,
                "topic_name": "Mạch điện",
                "parent_topic_id": None,
                "sections": [
                    {
                        "section_title": "Mạch điện đơn giản",
                        "contents": [
                            {
                                "content_type": "Quiz",
                                "content_title": "Quiz: Mạch điện"
                            },
                            {
                                "content_type": "Assignment",
                                "content_title": "Bài tập: Thiết kế mạch điện"
                            }
                        ]
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Mạch điện",
                        "lesson_description": "Bài học về mạch điện đơn giản"
                    }
                ]
            },
            {
                "topic_id": 5,
                "topic_name": "Điện học",
                "parent_topic_id": None,
                "sections": [
                    {
                        "section_title": "Mạch điện đơn giản",
                        "contents": [
                            {
                                "content_type": "Assignment",
                                "content_title": "Bài tập: Thiết kế mạch điện"
                            }
                        ]
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Mạch điện",
                        "lesson_description": "Bài học về mạch điện đơn giản"
                    }
                ]
            },
            {
                "topic_id": 6,
                "topic_name": "Dòng điện",
                "parent_topic_id": 5,
                "sections": [
                    {
                        "section_title": "Mạch điện đơn giản",
                        "contents": [
                            {
                                "content_type": "Assignment",
                                "content_title": "Bài tập: Thiết kế mạch điện"
                            }
                        ]
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Mạch điện",
                        "lesson_description": "Bài học về mạch điện đơn giản"
                    }
                ]
            },
            {
                "topic_id": 7,
                "topic_name": "Đơn vị đo",
                "parent_topic_id": None,
                "sections": [
                    {
                        "section_title": "Khái niệm về Lực",
                        "contents": []
                    }
                ],
                "lessons": [
                    {
                        "lesson_title": "Lực và Chuyển động",
                        "lesson_description": "Bài học về các loại lực và chuyển động"
                    }
                ]
            }
        ],
        "analysis_period": {
            "from_date": "2024-01-15T00:00:00Z",
            "to_date": "2024-01-25T23:59:59Z",
            "days_back": 7
        }
    }
