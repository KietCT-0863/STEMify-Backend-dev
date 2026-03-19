from abc import ABC, abstractmethod
from typing import Dict, Any, Optional


class ClassroomRepository(ABC):
    @abstractmethod
    async def get_classroom_data(
        self, 
        classroom_id: Optional[int] = None,
        student_id: Optional[str] = None,
        analysis_period_days: Optional[int] = None
    ) -> Dict[str, Any]:
        raise NotImplementedError

