"""
Recommendations Prompts
Prompt templates for student progress analysis and intervention recommendations
"""

from typing import Dict, Any
from app.infrastructure.config.settings import settings


def build_classroom_context(classroom_data: Dict[str, Any]) -> str:
    """
    Serialize classroom data into human-readable context for LLM analysis.
    Focuses on student progress, performance, and engagement metrics.
    Provides detailed data for accurate calculations.
    """
    lines = []
    
    # Classroom info
    classroom = classroom_data.get("classroom", {})
    lines.append(f"CLASSROOM: {classroom.get('name', 'Unknown')} (ID: {classroom.get('id')})")
    lines.append(f"Grade: {classroom.get('grade', 'N/A')}")
    lines.append("")
    
    # Students overview
    students = classroom_data.get("students", [])
    lines.append(f"STUDENTS ({len(students)} total in this prompt batch):")
    for student in students:
        lines.append(f"  - {student.get('student_name', 'Unknown')} (ID: {student.get('student_id')})")
    lines.append("")

    # Precomputed metrics (if available)
    precomputed_metrics = classroom_data.get("precomputed_metrics")
    if precomputed_metrics:
        lines.append("PRECOMPUTED METRICS PER STUDENT (use these values directly, do not recalculate):")
        for sid, metrics in precomputed_metrics.items():
            student_name = next((s.get("student_name") for s in students if s.get("student_id") == sid), "Unknown")
            lines.append(f"  {student_name} (ID: {sid}):")
            lines.append(f"    overall_progress_percentage = {metrics.get('overall_progress_percentage', 0.0):.2f}")
            lines.append(f"    average_score = {metrics.get('average_score', 0.0):.2f}")
            lines.append(f"    completion_rate = {metrics.get('completion_rate', 0.0):.3f}")
            lines.append(f"    engagement_score = {metrics.get('engagement_score', 0.0):.3f}")
            lines.append(f"    days_since_last_activity = {metrics.get('days_since_last_activity', 0)}")
            weak_topics = metrics.get("weak_topics", [])
            if weak_topics:
                lines.append("    weak_topics (precomputed):")
                for wt in weak_topics:
                    lines.append(
                        f"      - {wt.get('topic_name')}: mastery_score={wt.get('mastery_score', 0.0):.3f}, "
                        f"correct_rate={wt.get('correct_rate', 0.0):.3f}, attempts_count={wt.get('attempts_count', 0)}"
                    )
            lines.append("")
        lines.append("")
    
    # Enrollment progress (for overall_progress_percentage calculation)
    enrollments = classroom_data.get("enrollments", {})
    curriculum_enrollments = enrollments.get("curriculum_enrollments", [])
    course_enrollments = enrollments.get("course_enrollments", [])
    
    lines.append("ENROLLMENT PROGRESS (for overall_progress_percentage calculation):")
    for ce in curriculum_enrollments:
        student_id = ce.get("student_id")
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        lines.append(f"  {student_name}: Curriculum progress = {ce.get('progress_percentage', 0)}%")
    
    for ce in course_enrollments:
        student_id = ce.get("student_id")
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        lines.append(f"  {student_name}: Course progress = {ce.get('progress_percentage', 0)}%")
    lines.append("")
    
    # Detailed Quiz performance per student
    quizzes = classroom_data.get("quizzes", {})
    student_quizzes = quizzes.get("student_quizzes", [])
    quiz_attempts = quizzes.get("quiz_attempts", [])
    
    lines.append("QUIZ PERFORMANCE (for average_score calculation):")
    lines.append("  Formula: average_score = (sum of all quiz final_score + sum of all assignment final_score) / (count of quizzes with final_score + count of assignments with final_score)")
    lines.append("")
    
    # Group quiz attempts by student
    student_quiz_data = {}
    for sq in student_quizzes:
        student_id = sq.get("student_id")
        if student_id not in student_quiz_data:
            student_quiz_data[student_id] = {
                "quizzes": [],
                "attempts": []
            }
        student_quiz_data[student_id]["quizzes"].append(sq)
    
    for attempt in quiz_attempts:
        student_quiz_id = attempt.get("student_quiz_id")
        for sq in student_quizzes:
            if sq.get("id") == student_quiz_id:
                student_id = sq.get("student_id")
                if student_id in student_quiz_data:
                    student_quiz_data[student_id]["attempts"].append(attempt)
    
    for student_id, data in student_quiz_data.items():
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        lines.append(f"  {student_name} (ID: {student_id}):")
        
        # List all quizzes with final scores
        quiz_scores = []
        for sq in data["quizzes"]:
            final_score = sq.get("final_score")
            if final_score is not None:
                quiz_scores.append(final_score)
                lines.append(f"    Quiz '{sq.get('quiz_title', 'Unknown')}': final_score = {final_score}")
            else:
                lines.append(f"    Quiz '{sq.get('quiz_title', 'Unknown')}': status = {sq.get('status')}, final_score = None (not counted)")
        
        if quiz_scores:
            lines.append(f"    → Quiz scores to use in calculation: {quiz_scores}")
            lines.append(f"    → Sum of quiz scores: {sum(quiz_scores):.1f}")
            lines.append(f"    → Count of quizzes with scores: {len(quiz_scores)}")
        lines.append("")
    
    # Detailed Assignment performance per student
    assignments = classroom_data.get("assignments", {})
    student_assignments = assignments.get("student_assignments", [])
    assignment_attempts = assignments.get("assignment_attempts", [])
    
    lines.append("ASSIGNMENT PERFORMANCE (for average_score calculation):")
    
    # Group assignments by student
    student_assignment_data = {}
    for sa in student_assignments:
        student_id = sa.get("student_id")
        if student_id not in student_assignment_data:
            student_assignment_data[student_id] = []
        student_assignment_data[student_id].append(sa)
    
    for student_id, assignments_list in student_assignment_data.items():
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        lines.append(f"  {student_name} (ID: {student_id}):")
        
        assignment_scores = []
        for sa in assignments_list:
            final_score = sa.get("final_score")
            if final_score is not None:
                assignment_scores.append(final_score)
                lines.append(f"    Assignment '{sa.get('assignment_title', 'Unknown')}': final_score = {final_score}")
            else:
                lines.append(f"    Assignment '{sa.get('assignment_title', 'Unknown')}': status = {sa.get('status')}, final_score = None (not counted)")
        
        if assignment_scores:
            lines.append(f"    → Assignment scores to use in calculation: {assignment_scores}")
            lines.append(f"    → Sum of assignment scores: {sum(assignment_scores):.1f}")
            lines.append(f"    → Count of assignments with scores: {len(assignment_scores)}")
        lines.append("")
    
    # Engagement metrics (already provided, but make it clearer)
    time_metrics = classroom_data.get("time_metrics", {})
    engagement_metrics = time_metrics.get("engagement_metrics", [])
    
    lines.append("ENGAGEMENT METRICS (use these values directly):")
    for metric in engagement_metrics:
        student_id = metric.get("student_id")
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        days_since = metric.get("days_since_last_activity", 0)
        completion_rate = metric.get("completion_rate", 0)
        activities_7d = metric.get("total_activities_last_7_days", 0)
        lines.append(f"  {student_name} (ID: {student_id}):")
        lines.append(f"    days_since_last_activity = {days_since}")
        lines.append(f"    completion_rate = {completion_rate} (use this value directly)")
        lines.append(f"    engagement_score = {completion_rate} (use completion_rate as engagement_score)")
        lines.append(f"    total_activities_last_7_days = {activities_7d}")
    lines.append("")
    
    # Topics with question-level data for weak topics analysis
    lines.append("TOPICS AND QUESTION PERFORMANCE (for weak_topics calculation):")
    topics = classroom_data.get("topics", [])
    
    # Extract topic performance from quiz attempts
    topic_performance = {}
    for attempt in quiz_attempts:
        question_attempts = attempt.get("question_attempts", [])
        for qa in question_attempts:
            topics_list = qa.get("topics", [])
            is_correct = qa.get("is_correct", False)
            for topic_name in topics_list:
                if topic_name not in topic_performance:
                    topic_performance[topic_name] = {"correct": 0, "total": 0}
                topic_performance[topic_name]["total"] += 1
                if is_correct:
                    topic_performance[topic_name]["correct"] += 1
    
    # Group by student
    student_topic_performance = {}
    for sq in student_quizzes:
        student_id = sq.get("student_id")
        if student_id not in student_topic_performance:
            student_topic_performance[student_id] = {}
        
        # Find attempts for this quiz
        for attempt in quiz_attempts:
            if attempt.get("student_quiz_id") == sq.get("id"):
                question_attempts = attempt.get("question_attempts", [])
                for qa in question_attempts:
                    topics_list = qa.get("topics", [])
                    is_correct = qa.get("is_correct", False)
                    for topic_name in topics_list:
                        if topic_name not in student_topic_performance[student_id]:
                            student_topic_performance[student_id][topic_name] = {"correct": 0, "total": 0}
                        student_topic_performance[student_id][topic_name]["total"] += 1
                        if is_correct:
                            student_topic_performance[student_id][topic_name]["correct"] += 1
    
    for student_id, topics_data in student_topic_performance.items():
        student_name = next((s.get("student_name") for s in students if s.get("student_id") == student_id), "Unknown")
        lines.append(f"  {student_name} (ID: {student_id}):")
        for topic_name, stats in topics_data.items():
            correct_rate = stats["correct"] / stats["total"] if stats["total"] > 0 else 0
            mastery_score = correct_rate  # mastery = correct rate
            lines.append(f"    Topic '{topic_name}': {stats['correct']}/{stats['total']} correct = {correct_rate:.1%} (mastery_score = {mastery_score:.2f})")
        lines.append("")
    
    # Analysis period
    analysis_period = classroom_data.get("analysis_period", {})
    if analysis_period:
        lines.append("ANALYSIS PERIOD:")
        lines.append(f"  From: {analysis_period.get('from_date')}")
        lines.append(f"  To: {analysis_period.get('to_date')}")
        lines.append(f"  Days back: {analysis_period.get('days_back', 7)}")
        lines.append("")
    
    return "\n".join(lines)


def _get_default_intervention_prompt_template() -> str:
    """Return the default prompt template for intervention recommendations."""
    return """You are an expert educational consultant specializing in STEM education, particularly hands-on and project-based learning (similar to Khan Academy, ALEKS, IXL, and Renaissance Learning systems).

You are analyzing student progress data to identify learning gaps and recommend targeted interventions.

CONTEXT:
{classroom_context}

TASK:
You may receive only a subset of students (not the whole class). Analyze the provided student data and:
1. **Progress Assessment**: Calculate exact metrics using the formulas below, then identify weak areas and engagement patterns
2. **Intervention Recommendations**: Provide specific, actionable recommendations for each student who needs support

CRITICAL: CALCULATION REQUIREMENTS
You MUST calculate all metrics using the exact formulas below. DO NOT estimate or guess. Use the actual numbers provided in the context.

FORMULA 1: overall_progress_percentage
  - If both curriculum_enrollment and course_enrollment exist for a student:
    overall_progress_percentage = (curriculum_progress_percentage + course_progress_percentage) / 2
  - If only one exists, use that value directly
  - Example: curriculum=45%, course=50% → overall = (45+50)/2 = 47.5%

FORMULA 2: average_score
  - Collect ALL quiz final_score values (where final_score is not None)
  - Collect ALL assignment final_score values (where final_score is not None)
  - average_score = (sum of all quiz final_score + sum of all assignment final_score) / (count of quizzes with final_score + count of assignments with final_score)
  - Example: Quiz scores [85.5], Assignment scores [88.0] → average = (85.5 + 88.0) / (1 + 1) = 86.75
  - IMPORTANT: Only count items where final_score is not None. Do not count "Assigned", "UnderReview", or items without scores.

FORMULA 3: completion_rate
  - Use the completion_rate value directly from engagement_metrics
  - Do not calculate it yourself, use the provided value

FORMULA 4: engagement_score
  - engagement_score = completion_rate (from engagement_metrics)
  - Use the same value as completion_rate

FORMULA 5: days_since_last_activity
  - Use the days_since_last_activity value directly from engagement_metrics
  - Do not calculate it yourself

FORMULA 6: weak_topics (mastery_score and correct_rate)
  - For each topic, count correct answers and total attempts from question_attempts
  - mastery_score = correct_rate = (number of correct answers) / (total attempts)
  - attempts_count = total number of question attempts for that topic
  - Example: Topic "Lực" has 2 correct out of 3 attempts → mastery_score = 0.67, correct_rate = 0.67, attempts_count = 3
  - A topic is considered "weak" if mastery_score < 0.7 (70%)

CALCULATION STEPS (follow in order):
1. For each student, calculate overall_progress_percentage using FORMULA 1
2. For each student, calculate average_score using FORMULA 2
3. For each student, use completion_rate from engagement_metrics (FORMULA 3)
4. For each student, set engagement_score = completion_rate (FORMULA 4)
5. For each student, use days_since_last_activity from engagement_metrics (FORMULA 5)
6. For each student, calculate weak_topics using FORMULA 6
7. Only after calculating all metrics, proceed to generate recommendations

SCHEMA YOU MUST FOLLOW (INTERNAL REPORTS):
You MUST build an internal array called "reports". Each element in "reports" MUST follow this structure:

- reports[i].student_id: string (must match a student_id from the context)
- reports[i].student_name: string
- reports[i].summary: string (1-3 sentence executive summary for that student)
- reports[i].progress_metrics: object with:
  - student_id: string
  - student_name: string
  - overall_progress_percentage: number (0-100)
  - average_score: number (0-100)
  - completion_rate: number (0-1)
  - engagement_score: number (0-1)
  - days_since_last_activity: integer (>= 0)
  - weak_topics: array of objects, each with:
    - topic_id: integer (or null if not available)
    - topic_name: string
    - mastery_score: number (0-1)
    - attempts_count: integer (>= 0)
    - correct_rate: number (0-1)
- reports[i].recommendations: array of objects. Each recommendation MUST have:
  - type: one of ["remediation", "enrichment", "engagement", "practice", "peer_support", "teacher_attention"]
  - priority: one of ["critical", "high", "medium", "low"]
  - title: string
  - description: string
  - rationale: string
  - actionable_steps: array of strings
  - expected_outcome: string
  - related_topics: array of strings
  - estimated_duration_days: integer or null

TOP-LEVEL CLASS INSIGHTS SCHEMA (WHAT THE API EXPECTS):
In addition to "reports", you MUST also return a high-level overview for the UI:

- overviewText: string
- students: array of objects. Each student object MUST have:
  - studentId: string (must match a student_id from the context)
  - progressPercent: integer (0-100), derived from overall_progress_percentage
  - currentStatus: one of ["AtRisk", "Good", "NeedsSupport", "Excellent"]
  - statusText: string (1-3 sentences)
  - currentSection: object or null. If present, it MUST have:
    - sectionId: integer
    - sectionName: string
    - sectionStatus: one of ["InProgress", "Completed", "NotStarted"]
  - interventionText: string (short natural language description of the most important intervention)
- aiInsightsText: string (1-3 sentences about the class as a whole)

REQUIREMENTS (QUALITY & SAFETY):
- Focus on STEM hands-on and project-based learning approaches
- Recommendations should be practical and implementable by teachers
- Consider both remediation (for struggling students) and enrichment (for advanced students)
- Address engagement issues if students are inactive
- Provide clear rationale for each recommendation
- Include estimated timeframes for seeing improvement
- **DO NOT estimate metrics - calculate them using the formulas above**
- **Show your calculations mentally but return only the final JSON**

OUTPUT FORMAT (STRICT, SINGLE JSON OBJECT, NO COMMENTARY BEFORE OR AFTER):
{{
  "overviewText": "High-level summary of the class situation (1-3 sentences).",
  "students": [
    {{
      "studentId": "string",
      "progressPercent": 75,
      "currentStatus": "AtRisk",
      "statusText": "Natural language explanation of the student's situation, ~1-3 sentences.",
      "currentSection": {{
        "sectionId": 123,
        "sectionName": "Forces and Motion",
        "sectionStatus": "InProgress"
      }},
      "interventionText": "Short natural language description of the most important intervention for this student."
    }}
  ],
  "aiInsightsText": "Additional AI-generated insights about the class as a whole (1-3 sentences).",
  "reports": [
    {{
      "student_id": "string",
      "student_name": "string",
      "summary": "1-3 sentence executive summary for this student.",
      "progress_metrics": {{
        "student_id": "string",
        "student_name": "string",
        "overall_progress_percentage": 72.5,
        "average_score": 68.0,
        "completion_rate": 0.45,
        "engagement_score": 0.45,
        "days_since_last_activity": 5,
        "weak_topics": [
          {{
            "topic_id": 1,
            "topic_name": "Forces",
            "mastery_score": 0.6,
            "attempts_count": 5,
            "correct_rate": 0.6
          }}
        ]
      }},
      "recommendations": [
        {{
          "type": "remediation",
          "priority": "high",
          "title": "Targeted practice on forces and motion",
          "description": "Detailed description of what the teacher should do.",
          "rationale": "Why this matters based on the metrics.",
          "actionable_steps": [
            "Step 1 ...",
            "Step 2 ..."
          ],
          "expected_outcome": "What improvement we expect to see.",
          "related_topics": ["Forces"],
          "estimated_duration_days": 7
        }}
      ]
    }}
  ]
}}

IMPORTANT:
- **YOU MUST CALCULATE ALL METRICS USING THE FORMULAS ABOVE - DO NOT ESTIMATE**
- Provide at most {max_recommendations} recommendations per student who needs intervention
- Use "critical" priority only for students with severe issues (failing scores < 50%, no engagement)
- Use "high" priority for students significantly below average (scores 50-65%)
- Use "medium" priority for students who need moderate support (scores 65-75%)
- Use "low" priority for students performing well but could benefit from enrichment (scores > 85%)
- Be specific and actionable in recommendations
- **YOU MUST RETURN A VALID JSON OBJECT with top-level keys `overviewText`, `students`, `aiInsightsText`, and a `reports` array**
- **Only include students/reports for students present in the provided context**
- **DO NOT invent students that are not in the input**
- **DO NOT return an empty `students` or `reports` array if there is at least one student needing intervention**
- **DO NOT add any text before or after the JSON object**
"""


def build_intervention_prompt(classroom_context: str, lang: str = "vi") -> str:
    # Get template from settings or use default
    template = (
        settings.RECOMMENDATIONS_INTERVENTION_PROMPT_TEMPLATE
        or _get_default_intervention_prompt_template()
    )
    
    # Add language instruction to the prompt
    lang_instruction = ""
    if lang == "vi":
        lang_instruction = "\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in Vietnamese (Tiếng Việt). All text fields including overviewText, statusText, interventionText, aiInsightsText, summary, description, rationale, expected_outcome, title, and actionable_steps MUST be in Vietnamese."
    elif lang == "en":
        lang_instruction = "\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in English. All text fields including overviewText, statusText, interventionText, aiInsightsText, summary, description, rationale, expected_outcome, title, and actionable_steps MUST be in English."
    else:
        lang_instruction = f"\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in {lang}. All text fields including overviewText, statusText, interventionText, aiInsightsText, summary, description, rationale, expected_outcome, title, and actionable_steps MUST be in {lang}."
    template_with_lang = template + lang_instruction
    
    try:
        return template_with_lang.format(
            classroom_context=classroom_context,
            max_recommendations=settings.RECOMMENDATIONS_MAX_RECOMMENDATIONS_PER_STUDENT,
        )
    except KeyError:
        # Template doesn't have max_recommendations placeholder, only format classroom_context
        return template_with_lang.format(classroom_context=classroom_context)
