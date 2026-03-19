using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class ClassroomStudentConfiguration : BaseIntEntityConfiguration<ClassroomStudent>
    {
        public override void Configure(EntityTypeBuilder<ClassroomStudent> builder)
        {
            base.Configure(builder);
            builder.ToTable("ClassroomStudent");

            builder
                .Property(cs => cs.StudentId)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)")
                .HasComment("Identifier for the student");
        }
    }
}
