"""
Mock classroom repository for testing and fallback scenarios.
"""

import logging
from typing import Dict, Any, Optional
from datetime import datetime, timedelta

from app.core.data.classroom_repository import ClassroomRepository
from app.infrastructure.data.fixtures.mock_classroom_data import get_mock_classroom_data

logger = logging.getLogger(__name__)


class MockClassroomRepository(ClassroomRepository):
    """Mock repository that returns static test data."""

    async def get_classroom_data(
        self,
        classroom_id: Optional[int] = None,
        student_id: Optional[str] = None,
        analysis_period_days: Optional[int] = None
    ) -> Dict[str, Any]:
        """Return mock classroom data."""
        logger.info("Using mock classroom data", extra={"classroom_id": classroom_id})
        
        data = get_mock_classroom_data()
        
        # Adjust analysis period if specified
        if analysis_period_days:
            to_date = datetime.utcnow()
            from_date = to_date - timedelta(days=analysis_period_days)
            
            data["analysis_period"] = {
                "from_date": from_date.isoformat() + "Z",
                "to_date": to_date.isoformat() + "Z",
                "days_back": analysis_period_days,
            }
        
        # Filter by student_id if specified
        if student_id:
            # Filter students
            students = [s for s in data.get("students", []) if s.get("student_id") == student_id]
            data["students"] = students
            
            # Filter enrollments, progress, quizzes, assignments by student_id
            # This is a simplified filter - in production, you'd want more comprehensive filtering
            for key in ["enrollments", "progress", "quizzes", "assignments"]:
                if key in data:
                    # Apply student filtering logic here if needed
                    pass
        
        return data





