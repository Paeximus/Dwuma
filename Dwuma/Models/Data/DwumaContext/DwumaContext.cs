using DocumentFormat.OpenXml.Bibliography;
using Dwuma.Models.Enums;
using Dwuma.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Dwuma.Models.Data.DwumaContext;

public partial class DwumaContext : DbContext
{
    public DwumaContext(DbContextOptions<DwumaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<CvDocument> CvDocuments { get; set; }

    public virtual DbSet<InterviewQa> InterviewQas { get; set; }

    public virtual DbSet<InterviewSession> InterviewSessions { get; set; }

    public virtual DbSet<JobInteraction> JobInteractions { get; set; }

    public virtual DbSet<JobListing> JobListings { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationEngagement> NotificationEngagements { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<SkillGapItem> SkillGapItems { get; set; }

    public virtual DbSet<SkillGapReport> SkillGapReports { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Notification> Notification { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__APPLICAT__3213E83F07ECED42");

            entity.ToTable("APPLICATIONS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("applied_at");
            entity.Property(e => e.AutoSubmitted)
                .HasDefaultValue(false)
                .HasColumnName("auto_submitted");
            entity.Property(e => e.CvDocumentId).HasColumnName("cv_document_id");
            entity.Property(e => e.JobListingId).HasColumnName("job_listing_id");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                .HasColumnName("last_updated");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("applied")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CvDocument).WithMany(p => p.Applications)
                .HasForeignKey(d => d.CvDocumentId)
                .HasConstraintName("FK__APPLICATI__cv_do__6383C8BA");

            entity.HasOne(d => d.JobListing).WithMany(p => p.Applications)
                .HasForeignKey(d => d.JobListingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__APPLICATI__job_l__628FA481");

            entity.HasOne(d => d.User).WithMany(p => p.Applications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__APPLICATI__user___619B8048");
        });

        modelBuilder.Entity<CvDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CV_DOCUM__3213E83F8D8B2E4B");

            entity.ToTable("CV_DOCUMENTS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Changelog)
                .HasColumnType("text")
                .HasColumnName("changelog");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(512)
                .IsUnicode(false)
                .HasColumnName("file_path");
            entity.Property(e => e.FileType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("file_type");
            entity.Property(e => e.IsBaseCv)
                .HasDefaultValue(false)
                .HasColumnName("is_base_cv");
            entity.Property(e => e.JobListingId).HasColumnName("job_listing_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.JobListing).WithMany(p => p.CvDocuments)
                .HasForeignKey(d => d.JobListingId)
                .HasConstraintName("FK__CV_DOCUME__job_l__5CD6CB2B");

            entity.HasOne(d => d.User).WithMany(p => p.CvDocuments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__CV_DOCUME__user___5BE2A6F2");
        });

        modelBuilder.Entity<InterviewQa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__INTERVIE__3213E83F8D33692A");

            entity.ToTable("INTERVIEW_QA");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Feedback)
                .HasColumnType("text")
                .HasColumnName("feedback");
            entity.Property(e => e.Question)
                .HasColumnType("text")
                .HasColumnName("question");
            entity.Property(e => e.QuestionOrder).HasColumnName("question_order");
            entity.Property(e => e.ResponseMode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("response_mode");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.UserResponse)
                .HasColumnType("text")
                .HasColumnName("user_response");

            entity.HasOne(d => d.Session).WithMany(p => p.InterviewQas)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__INTERVIEW__sessi__6EF57B66");
        });

        modelBuilder.Entity<InterviewSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__INTERVIE__3213E83F4CD8A7E0");

            entity.ToTable("INTERVIEW_SESSIONS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Company)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("company");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.JobListingId).HasColumnName("job_listing_id");
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("started_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.JobListing).WithMany(p => p.InterviewSessions)
                .HasForeignKey(d => d.JobListingId)
                .HasConstraintName("FK__INTERVIEW__job_l__6B24EA82");

            entity.HasOne(d => d.User).WithMany(p => p.InterviewSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__INTERVIEW__user___6A30C649");
        });

        modelBuilder.Entity<JobInteraction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JobInter__3214EC07741AEAE2");

            entity.ToTable("JOB_INTERACTIONS");

            entity.HasIndex(e => new { e.UserId, e.JobListingId }, "UQ_JobInteractions_User_Job").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.JobListing).WithMany(p => p.JobInteractions)
                .HasForeignKey(d => d.JobListingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobInteractions_JobListings");

            entity.HasOne(d => d.User).WithMany(p => p.JobInteractions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobInteractions_Users");
        });

        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JOB_LIST__3213E83F6EAC5573");

            entity.ToTable("JOB_LISTINGS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Company)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("company");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DiscoveredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("discovered_at");
            entity.Property(e => e.JobType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("job_type");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.RelevanceScore).HasColumnName("relevance_score");
            entity.Property(e => e.SourceUrl)
                .HasMaxLength(512)
                .IsUnicode(false)
                .HasColumnName("source_url");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("discovered")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(255)
                .HasColumnName("external_id");
            entity.Property(e => e.Industry)
                .HasMaxLength(150)
                .HasColumnName("industry");
            entity.Property(e => e.RequiredSkills)
        .HasColumnType("text")
        .HasColumnName("required_skills");
            entity.Property(e => e.Salary)
        .HasMaxLength(255)
        .HasColumnName("salary");
            entity.Property(e => e.IsRemote)
        .HasColumnType("tinyint(1)")
        .HasDefaultValue(false)
        .HasColumnName("is_remote");
            entity.Property(e => e.Source)
        .HasMaxLength(100)
        .HasColumnName("source");
            entity.Property(e => e.PostedAt)
        .HasColumnType("datetime")
        .HasColumnName("posted_at");
            entity.Property(e => e.ExpiresAt)
        .HasColumnType("datetime")
        .HasColumnName("expires_at");
            entity.HasIndex(e => new
            {
                e.Source,
                e.ExternalId
            })
    .IsUnique()
    .HasDatabaseName("UX_JOB_LISTINGS_SOURCE_EXTERNAL_ID");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.ToTable("NOTIFICATIONS");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("user_id");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.Content)
                .HasMaxLength(4000)
                .HasColumnName("content");

            entity.Property(e => e.NotificationType)
                .HasMaxLength(100)
                .HasColumnName("notification_type");

            entity.Property(e => e.ScheduledAt)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_at");

            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasColumnName("sent_at");

            entity.Property(e => e.WasEngaged)
                .HasColumnName("was_engaged");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_NOTIFICATIONS_USERS");
        });

        modelBuilder.Entity<NotificationEngagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NOTIFICA__3213E83F99BB17C8");

            entity.ToTable("NOTIFICATION_ENGAGEMENT");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.Engaged)
                .HasDefaultValue(false)
                .HasColumnName("engaged");
            entity.Property(e => e.HourOfDay).HasColumnName("hour_of_day");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("recorded_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationEngagements)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__NOTIFICAT__notif__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationEngagements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NOTIFICAT__user___7D439ABD");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PROFILES__3213E83F39B64B2F");

            entity.ToTable("PROFILES");

            entity.HasIndex(e => e.UserId, "UQ__PROFILES__B9BE370EE4EE32F9").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CareerGoals)
                .HasColumnType("text")
                .HasColumnName("career_goals");
            entity.Property(e => e.Degree)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("degree");
            entity.Property(e => e.FieldOfStudy)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("field_of_study");
            entity.Property(e => e.GpaClassification)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("gpa_classification");
            entity.Property(e => e.GraduationYear).HasColumnName("graduation_year");
            entity.Property(e => e.Institution)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("institution");
            entity.Property(e => e.JobTypePreference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("job_type_preference");
            entity.Property(e => e.LocationPreference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("location_preference");
            entity.Property(e => e.PreferredIndustries)
                .HasColumnType("text")
                .HasColumnName("preferred_industries");
            entity.Property(e => e.SalaryExpectation)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("salary_expectation");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("FK__PROFILES__user_i__4F7CD00D");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SKILLS__3213E83FCB4D529D");

            entity.ToTable("SKILLS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("added_at");
            entity.Property(e => e.ProficiencyLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("proficiency_level");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("skill_name");
            entity.Property(e => e.SkillType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("skill_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Skills)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SKILLS__user_id__534D60F1");
        });

        modelBuilder.Entity<SkillGapItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SKILL_GA__3213E83F1704B9DA");

            entity.ToTable("SKILL_GAP_ITEMS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GapStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("gap_status");
            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.RequiredLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("required_level");
            entity.Property(e => e.ResourceName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("resource_name");
            entity.Property(e => e.ResourceUrl)
                .HasMaxLength(512)
                .IsUnicode(false)
                .HasColumnName("resource_url");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("skill_name");

            entity.HasOne(d => d.Report).WithMany(p => p.SkillGapItems)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("FK__SKILL_GAP__repor__75A278F5");
        });

        modelBuilder.Entity<SkillGapReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SKILL_GA__3213E83FE1A02508");

            entity.ToTable("SKILL_GAP_REPORTS");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CareerPath)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("career_path");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("generated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SkillGapReports)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__SKILL_GAP__user___71D1E811");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__USERS__3213E83F56B3AC3A");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Email, "UQ__USERS__AB6E6164B26437FC").IsUnique();
            entity.HasIndex(e => e.FullName, "UQ_USERS_FULL_NAME").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");

            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.JwtToken)
                .HasMaxLength(512)
                .IsUnicode(false)
                .HasColumnName("jwt_token");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            
            entity.Property(e => e.IsEmailVerified)
                .HasColumnName("is_email_verified")
                .HasDefaultValue(false);

            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnName("email_verified_at");

            entity.Property(e => e.OnboardingStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnName("onboarding_status")
                .HasDefaultValue(OnboardingStatus.NotStarted);

            entity.Property(e => e.OnboardingCompletedAt)
                .HasColumnName("onboarding_completed_at");

            entity.Property(e => e.EmailVerificationTokenHash)
                .HasColumnName("email_verification_token_hash")
                .HasMaxLength(128);

            entity.Property(e => e.EmailVerificationExpiresAt)
                .HasColumnName("email_verification_expires_at")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
