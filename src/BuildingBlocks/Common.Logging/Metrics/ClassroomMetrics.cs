using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;

/// <summary>
/// Metrics for Classroom Service - Classroom management, assignments, and student engagement
/// Following OpenTelemetry semantic conventions and best practices
/// </summary>
public static class ClassroomMetrics
{
    private static readonly Meter Meter = new("STEMify.Classroom", "1.0.0");

    #region Classroom Metrics

    /// <summary>
    /// Total number of classrooms created
    /// Labels: classroom_type (course, workshop, lab)
    /// </summary>
    public static readonly Counter<long> ClassroomsCreated = Meter.CreateCounter<long>(
        "classroom.classrooms.created.total",
        unit: "{classrooms}",
        description: "Total number of classrooms created"
    );

    /// <summary>
    /// Current number of active classrooms
    /// </summary>
    public static readonly ObservableGauge<int> ActiveClassrooms = Meter.CreateObservableGauge(
        "classroom.classrooms.active",
        () => GetActiveClassroomsCount(),
        unit: "{classrooms}",
        description: "Current number of active classrooms"
    );

    private static Func<int> _activeClassroomsCountProvider = () => 0;

    public static void SetActiveClassroomsCountProvider(Func<int> provider)
    {
        _activeClassroomsCountProvider = provider;
    }

    private static int GetActiveClassroomsCount() => _activeClassroomsCountProvider();

    #endregion

    #region Student Enrollment Metrics

    /// <summary>
    /// Total number of student enrollments
    /// Labels: enrollment_type (direct, invitation, bulk)
    /// </summary>
    public static readonly Counter<long> StudentEnrollments = Meter.CreateCounter<long>(
        "classroom.enrollments.total",
        unit: "{enrollments}",
        description: "Total number of student enrollments"
    );

    /// <summary>
    /// Total number of student withdrawals
    /// Labels: withdrawal_reason (self_initiated, instructor_removed, course_ended)
    /// </summary>
    public static readonly Counter<long> StudentWithdrawals = Meter.CreateCounter<long>(
        "classroom.withdrawals.total",
        unit: "{withdrawals}",
        description: "Total number of student withdrawals"
    );

    /// <summary>
    /// Average classroom size
    /// </summary>
    public static readonly ObservableGauge<double> AverageClassroomSize = Meter.CreateObservableGauge(
        "classroom.size.average",
        () => GetAverageClassroomSize(),
        unit: "{students}",
        description: "Average number of students per classroom"
    );

    private static Func<double> _averageClassroomSizeProvider = () => 0;

    public static void SetAverageClassroomSizeProvider(Func<double> provider)
    {
        _averageClassroomSizeProvider = provider;
    }

    private static double GetAverageClassroomSize() => _averageClassroomSizeProvider();

    #endregion

    #region Assignment Metrics

    /// <summary>
    /// Total number of assignments created
    /// Labels: assignment_type (homework, quiz, project, lab)
    /// </summary>
    public static readonly Counter<long> AssignmentsCreated = Meter.CreateCounter<long>(
        "classroom.assignments.created.total",
        unit: "{assignments}",
        description: "Total number of assignments created"
    );

    /// <summary>
    /// Total number of assignment submissions
    /// Labels: submission_status (on_time, late, resubmission)
    /// </summary>
    public static readonly Counter<long> AssignmentSubmissions = Meter.CreateCounter<long>(
        "classroom.assignments.submissions.total",
        unit: "{submissions}",
        description: "Total number of assignment submissions"
    );

    /// <summary>
    /// Total number of assignments graded
    /// Labels: grading_method (auto, manual, peer_review)
    /// </summary>
    public static readonly Counter<long> AssignmentsGraded = Meter.CreateCounter<long>(
        "classroom.assignments.graded.total",
        unit: "{assignments}",
        description: "Total number of assignments graded"
    );

    /// <summary>
    /// Duration of assignment grading
    /// Labels: grading_method, assignment_type
    /// </summary>
    public static readonly Histogram<double> GradingDuration = Meter.CreateHistogram<double>(
        "classroom.grading.duration",
        unit: "s",
        description: "Duration of assignment grading in seconds"
    );

    #endregion

    #region Session Metrics

    /// <summary>
    /// Total number of classroom sessions started
    /// Labels: session_type (live, recorded, hybrid)
    /// </summary>
    public static readonly Counter<long> SessionsStarted = Meter.CreateCounter<long>(
        "classroom.sessions.started.total",
        unit: "{sessions}",
        description: "Total number of classroom sessions started"
    );

    /// <summary>
    /// Current number of active sessions
    /// </summary>
    public static readonly ObservableGauge<int> ActiveSessions = Meter.CreateObservableGauge(
        "classroom.sessions.active",
        () => GetActiveSessionsCount(),
        unit: "{sessions}",
        description: "Current number of active classroom sessions"
    );

    /// <summary>
    /// Total session duration
    /// Labels: session_type
    /// </summary>
    public static readonly Counter<double> SessionDuration = Meter.CreateCounter<double>(
        "classroom.sessions.duration.total",
        unit: "s",
        description: "Total duration of classroom sessions in seconds"
    );

    /// <summary>
    /// Total number of session participants
    /// Labels: participant_role (student, teacher, observer)
    /// </summary>
    public static readonly Counter<long> SessionParticipants = Meter.CreateCounter<long>(
        "classroom.sessions.participants.total",
        unit: "{participants}",
        description: "Total number of session participants"
    );

    private static Func<int> _activeSessionsCountProvider = () => 0;

    public static void SetActiveSessionsCountProvider(Func<int> provider)
    {
        _activeSessionsCountProvider = provider;
    }

    private static int GetActiveSessionsCount() => _activeSessionsCountProvider();

    #endregion

    #region Engagement Metrics

    /// <summary>
    /// Total number of student interactions
    /// Labels: interaction_type (comment, question, answer, reaction)
    /// </summary>
    public static readonly Counter<long> StudentInteractions = Meter.CreateCounter<long>(
        "classroom.interactions.total",
        unit: "{interactions}",
        description: "Total number of student interactions"
    );

    /// <summary>
    /// Total number of resource accesses
    /// Labels: resource_type (video, document, quiz, simulation)
    /// </summary>
    public static readonly Counter<long> ResourceAccesses = Meter.CreateCounter<long>(
        "classroom.resources.accesses.total",
        unit: "{accesses}",
        description: "Total number of resource accesses"
    );

    /// <summary>
    /// Student attendance rate
    /// </summary>
    public static readonly ObservableGauge<double> AttendanceRate = Meter.CreateObservableGauge(
        "classroom.attendance.rate",
        () => GetAttendanceRate(),
        unit: "1",
        description: "Student attendance rate (0-1)"
    );

    private static Func<double> _attendanceRateProvider = () => 0;

    public static void SetAttendanceRateProvider(Func<double> provider)
    {
        _attendanceRateProvider = provider;
    }

    private static double GetAttendanceRate() => _attendanceRateProvider();

    #endregion

    #region Helper Methods

    public static void RecordClassroomCreated(string classroomType)
    {
        ClassroomsCreated.Add(1,
            new KeyValuePair<string, object?>("classroom_type", classroomType));
    }

    public static void RecordStudentEnrollment(string enrollmentType)
    {
        StudentEnrollments.Add(1,
            new KeyValuePair<string, object?>("enrollment_type", enrollmentType));
    }

    public static void RecordAssignmentCreated(string assignmentType)
    {
        AssignmentsCreated.Add(1,
            new KeyValuePair<string, object?>("assignment_type", assignmentType));
    }

    public static void RecordAssignmentSubmission(string submissionStatus)
    {
        AssignmentSubmissions.Add(1,
            new KeyValuePair<string, object?>("submission_status", submissionStatus));
    }

    public static void RecordAssignmentGraded(string gradingMethod, string assignmentType, TimeSpan duration)
    {
        AssignmentsGraded.Add(1,
            new KeyValuePair<string, object?>("grading_method", gradingMethod));

        GradingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("grading_method", gradingMethod),
            new KeyValuePair<string, object?>("assignment_type", assignmentType));
    }

    public static void RecordSessionStarted(string sessionType)
    {
        SessionsStarted.Add(1,
            new KeyValuePair<string, object?>("session_type", sessionType));
    }

    public static void RecordSessionDuration(string sessionType, TimeSpan duration)
    {
        SessionDuration.Add(duration.TotalSeconds,
            new KeyValuePair<string, object?>("session_type", sessionType));
    }

    public static void RecordStudentInteraction(string interactionType)
    {
        StudentInteractions.Add(1,
            new KeyValuePair<string, object?>("interaction_type", interactionType));
    }

    #endregion
}
