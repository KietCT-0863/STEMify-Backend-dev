"""
Recommendations Models
Domain models for student progress analysis and intervention recommendations
"""

from typing import List, Optional
from datetime import datetime
from enum import Enum

from pydantic import BaseModel, Field


class InterventionPriority(str, Enum):
    """Priority level for interventions"""
    CRITICAL = "critical"
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"


class InterventionType(str, Enum):
    """Type of intervention"""
    REMEDIATION = "remediation"  # Hỗ trợ học lại
    ENRICHMENT = "enrichment"  # Mở rộng kiến thức
    ENGAGEMENT = "engagement"  # Tăng động lực
    PRACTICE = "practice"  # Luyện tập thêm
    PEER_SUPPORT = "peer_support"  # Hỗ trợ từ bạn học
    TEACHER_ATTENTION = "teacher_attention"  # Cần giáo viên chú ý


class StudentProgressRequest(BaseModel):
    """
    Request model for analyzing student progress and generating interventions.
    """
    
    classroom_id: Optional[int] = Field(
        default=None,
        description="ID of the classroom to analyze. If None, uses mock data.",
    )
    student_id: Optional[str] = Field(
        default=None,
        description="Specific student ID to analyze. If None, analyzes all students.",
    )
    force_mock: bool = Field(
        default=False,
        description="Force using mock data regardless of classroom_id (useful for testing).",
    )
    analysis_period_days: Optional[int] = Field(
        default=7,
        ge=1,
        le=90,
        description="Number of days to look back for analysis (default: 7 days).",
    )
    lang: Optional[str] = Field(
        default="vi",
        )


class WeakTopic(BaseModel):
    """Identified weak topic for a student"""
    
    topic_id: int = Field(..., description="Topic identifier")
    topic_name: str = Field(..., description="Topic name")
    mastery_score: float = Field(..., ge=0.0, le=1.0, description="Mastery score (0-1)")
    attempts_count: int = Field(..., ge=0, description="Number of attempts")
    correct_rate: float = Field(..., ge=0.0, le=1.0, description="Correct answer rate")
    last_attempt_date: Optional[datetime] = Field(default=None, description="Last attempt timestamp")


class StudentProgressMetrics(BaseModel):
    """Progress metrics for a student"""
    
    student_id: str = Field(..., description="Student identifier")
    student_name: str = Field(..., description="Student name")
    overall_progress_percentage: float = Field(..., ge=0.0, le=100.0, description="Overall progress %")
    average_score: float = Field(..., ge=0.0, le=100.0, description="Average score across all activities")
    completion_rate: float = Field(..., ge=0.0, le=1.0, description="Completion rate (0-1)")
    engagement_score: float = Field(..., ge=0.0, le=1.0, description="Engagement score (0-1)")
    weak_topics: List[WeakTopic] = Field(default_factory=list, description="List of weak topics")
    days_since_last_activity: int = Field(..., ge=0, description="Days since last activity")


class InterventionRecommendation(BaseModel):
    """A single intervention recommendation"""
    
    type: InterventionType = Field(..., description="Type of intervention")
    priority: InterventionPriority = Field(..., description="Priority level")
    title: str = Field(..., description="Short title of the recommendation")
    description: str = Field(..., description="Detailed description of the intervention")
    rationale: str = Field(..., description="Why this intervention is recommended")
    actionable_steps: List[str] = Field(..., description="Step-by-step actions to take")
    expected_outcome: str = Field(..., description="Expected outcome if intervention is applied")
    related_topics: List[str] = Field(default_factory=list, description="Related topics to focus on")
    estimated_duration_days: Optional[int] = Field(
        default=None,
        ge=1,
        description="Estimated duration in days to see improvement",
    )


class StudentInterventionReport(BaseModel):
    """Complete intervention report for a student"""
    
    student_id: str = Field(..., description="Student identifier")
    student_name: str = Field(..., description="Student name")
    analysis_date: datetime = Field(default_factory=datetime.utcnow, description="Analysis timestamp")
    progress_metrics: StudentProgressMetrics = Field(..., description="Student progress metrics")
    recommendations: List[InterventionRecommendation] = Field(
        default_factory=list,
        description="List of intervention recommendations",
    )
    summary: str = Field(..., description="Executive summary of student's situation")


class StudentCurrentSection(BaseModel):
    """Current learning section/module of the student (for high-level insight UI)."""

    sectionId: int = Field(..., description="Section identifier")
    sectionName: str = Field(..., description="Section name")
    sectionStatus: str = Field(..., description="Section status (e.g., InProgress, Completed)")


class StudentOverview(BaseModel):
    """
    High-level AI overview for a single student.

    This model is shaped to match the AI_Analysis.json contract used by the frontend.
    """

    studentId: str = Field(..., description="Student identifier")
    progressPercent: float = Field(..., ge=0.0, le=100.0, description="Overall progress percentage (0-100)")
    currentStatus: str = Field(..., description="High-level status label (e.g., AtRisk, Good)")
    statusText: str = Field(..., description="Natural language explanation of the student's status")
    currentSection: Optional[StudentCurrentSection] = Field(
        default=None,
        description="Optional current section/module the student is working on",
    )
    interventionText: str = Field(
        ...,
        description="Short natural language recommendation / intervention summary for the student",
    )


class InterventionResponse(BaseModel):

    overviewText: str = Field(..., description="Global classroom-level overview text")
    students: List[StudentOverview] = Field(..., description="Per-student AI insights")
    aiInsightsText: str = Field(..., description="Additional AI-generated insights for the class")
