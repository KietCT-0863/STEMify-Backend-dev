using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Classroom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classroom",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Name = table.Column<string>(
                        type: "varchar(255)",
                        maxLength: 255,
                        nullable: false,
                        comment: "Classroom display name"
                    ),
                    Grade = table.Column<string>(
                        type: "varchar(50)",
                        maxLength: 50,
                        nullable: false,
                        comment: "Grade level (e.g., Grade 1, Grade 2)"
                    ),
                    Description = table.Column<string>(
                        type: "varchar(1000)",
                        maxLength: 1000,
                        nullable: true,
                        comment: "Classroom description"
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP",
                        comment: "Record creation timestamp"
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP",
                        comment: "Record last update timestamp"
                    ),
                    StartDate = table.Column<DateOnly>(
                        type: "date",
                        nullable: false,
                        comment: "Classroom start date"
                    ),
                    EndDate = table.Column<DateOnly>(
                        type: "date",
                        nullable: false,
                        comment: "Classroom end date"
                    ),
                    TeacherId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false,
                        comment: "Foreign key to teacher (Identity service)"
                    ),
                    ClassCode = table.Column<string>(
                        type: "varchar(100)",
                        maxLength: 100,
                        nullable: false,
                        comment: "Unique classroom code for joining"
                    ),
                    CoverImageUrl = table.Column<string>(
                        type: "varchar(200)",
                        maxLength: 200,
                        nullable: true,
                        comment: "URL to classroom cover image"
                    ),
                    Status = table.Column<string>(
                        type: "varchar(50)",
                        nullable: false,
                        comment: "Classroom status (Active, Inactive, Archived)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classroom", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Enrollment",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    StudentId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false,
                        comment: "Foreign key to student (Identity service)"
                    ),
                    CourseId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Foreign key to course"
                    ),
                    EnrolledAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CompletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Status = table.Column<string>(
                        type: "varchar(50)",
                        nullable: false,
                        comment: "Enrollment status (InProgress, Compeleted)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollment", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(
                        type: "bytea",
                        rowVersion: true,
                        nullable: true
                    ),
                    Received = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Consumed = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Delivered = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint(
                        "AK_InboxState_MessageId_ConsumerId",
                        x => new { x.MessageId, x.ConsumerId }
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(
                        type: "bytea",
                        rowVersion: true,
                        nullable: true
                    ),
                    Created = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Delivered = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ClassroomId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Foreign key to classroom"
                    ),
                    Title = table.Column<string>(
                        type: "varchar(255)",
                        maxLength: 255,
                        nullable: false,
                        comment: "Announcement title"
                    ),
                    Content = table.Column<string>(
                        type: "varchar(2000)",
                        maxLength: 2000,
                        nullable: false,
                        comment: "Announcement content"
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP",
                        comment: "Announcement creation timestamp"
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "CURRENT_TIMESTAMP",
                        comment: "Announcement last update timestamp"
                    ),
                    FileUrl = table.Column<string>(
                        type: "varchar(500)",
                        maxLength: 500,
                        nullable: true,
                        comment: "Optional file attachment URL"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Classroom",
                        column: x => x.ClassroomId,
                        principalTable: "Classroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ClassroomResource",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    ClassroomId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Foreign key to classroom"
                    ),
                    CourseId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Foreign key to course"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomResources_Classroom",
                        column: x => x.ClassroomId,
                        principalTable: "Classroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "StudentLessonProgress",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    LessonId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Reference to Lesson (from resource-service)"
                    ),
                    Status = table.Column<string>(
                        type: "varchar(50)",
                        maxLength: 50,
                        nullable: false,
                        comment: "Progress status: NotStarted, InProgress, Completed, Submitted, Failed."
                    ),
                    CompletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true,
                        comment: "Timestamp when student completed the lesson"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLessonProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentLessonProgress_Enrollment_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    EnqueueTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    SentTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    DestinationAddress = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    ResponseAddress = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    FaultAddress = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    ExpirationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" }
                    );
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "StudentSectionProgress",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    StudentLessonProgressId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "FK to StudentLessonProgress"
                    ),
                    SectionId = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "FK to Section (from resource-service)"
                    ),
                    CompletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true,
                        comment: "Timestamp when the section was completed"
                    ),
                    Status = table.Column<string>(
                        type: "varchar(50)",
                        nullable: false,
                        comment: "Progress status: NotStarted, InProgress, Completed, Submitted, Failed."
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSectionProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSectionProgress_StudentLessonProgress_StudentLessonP~",
                        column: x => x.StudentLessonProgressId,
                        principalTable: "StudentLessonProgress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ClassroomCreatedAt",
                table: "Announcement",
                columns: new[] { "ClassroomId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ClassroomId",
                table: "Announcement",
                column: "ClassroomId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CreatedAt",
                table: "Announcement",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_ClassCode_Unique",
                table: "Classroom",
                column: "ClassCode",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_DateRange",
                table: "Classroom",
                columns: new[] { "StartDate", "EndDate" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_Status",
                table: "Classroom",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_TeacherId",
                table: "Classroom",
                column: "TeacherId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ClassroomId",
                table: "ClassroomResource",
                column: "ClassroomId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ClassroomResource_Unique",
                table: "ClassroomResource",
                columns: new[] { "ClassroomId", "CourseId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomResources_ResourceId",
                table: "ClassroomResource",
                column: "CourseId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollment",
                column: "CourseId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseStudent_Unique",
                table: "Enrollment",
                columns: new[] { "CourseId", "StudentId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Status",
                table: "Enrollment",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollment",
                column: "StudentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonProgress_EnrollmentId",
                table: "StudentLessonProgress",
                column: "EnrollmentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonProgress_Status",
                table: "StudentLessonProgress",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionProgress_LessonProgressId",
                table: "StudentSectionProgress",
                column: "StudentLessonProgressId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionProgress_LessonSection_Unique",
                table: "StudentSectionProgress",
                columns: new[] { "StudentLessonProgressId", "SectionId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionProgress_SectionId",
                table: "StudentSectionProgress",
                column: "SectionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionProgress_Status",
                table: "StudentSectionProgress",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Announcement");

            migrationBuilder.DropTable(name: "ClassroomResource");

            migrationBuilder.DropTable(name: "OutboxMessage");

            migrationBuilder.DropTable(name: "StudentSectionProgress");

            migrationBuilder.DropTable(name: "Classroom");

            migrationBuilder.DropTable(name: "InboxState");

            migrationBuilder.DropTable(name: "OutboxState");

            migrationBuilder.DropTable(name: "StudentLessonProgress");

            migrationBuilder.DropTable(name: "Enrollment");
        }
    }
}
