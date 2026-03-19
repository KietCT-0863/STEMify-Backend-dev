using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;
using System.Text.Json;

namespace Order.Infrastructure.Configurations
{
    public class SubscriptionOrderCurriculumConfiguration : BaseIntEntityConfiguration<SubscriptionOrderCurriculum>
    {
        public override void Configure(EntityTypeBuilder<SubscriptionOrderCurriculum> builder)
        {
            base.Configure(builder);
            builder.ToTable("SubscriptionOrderCurriculums");
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            };
            builder.HasOne(x => x.OrganizationSubscriptionOrder)
                   .WithMany(x => x.SubscriptionOrderCurriculums)
                   .HasForeignKey(x => x.OrganizationSubscriptionOrderId);

            builder.Property(x => x.CoursesSnapshot)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<CourseSnapshot>>(v, jsonOptions) ?? new()
                )
                .HasColumnType("jsonb");
            builder.Property(x => x.EmulatorsSnapshot)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<EmulatorSnapshot>>(v, jsonOptions) ?? new()
                )
                .HasColumnType("jsonb");
        }
    }
}
