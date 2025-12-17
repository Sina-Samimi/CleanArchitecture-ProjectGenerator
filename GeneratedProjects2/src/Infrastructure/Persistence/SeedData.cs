using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using LogsDtoCloneTest.Domain.Constants;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace LogsDtoCloneTest.Infrastructure.Persistence;

internal static class SeedData
{
    private static readonly DateTimeOffset DefaultAuditTimestamp = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IPAddress SeedIpAddress = IPAddress.Parse("127.0.0.1");

    internal static readonly ApplicationUser SystemUser = new()
    {
        Id = SystemUsers.AutomationId,
        UserName = "system@arsis.local",
        NormalizedUserName = "SYSTEM@ARSIS.LOCAL",
        Email = "system@arsis.local",
        NormalizedEmail = "SYSTEM@ARSIS.LOCAL",
        EmailConfirmed = true,
        SecurityStamp = "SYSTEM-SECURITY-STAMP",
        ConcurrencyStamp = "SYSTEM-CONCURRENCY-STAMP",
        FullName = "System Automation",
        IsActive = true,
        CreatedOn = DefaultAuditTimestamp,
        LastModifiedOn = DefaultAuditTimestamp,
        LockoutEnabled = false
    };

    // Fixed IDs for roles to ensure consistent seeding
    private const string AdminRoleId = "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d";
    private const string SellerRoleId = "b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e";
    private const string UserRoleId = "c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f";
    private const string AuthorRoleId = "d4e5f6a7-b8c9-7d8e-1f2a-3b4c5d6e7f8a";

    internal static readonly IReadOnlyCollection<IdentityRole> Roles = new[]
    {
        new IdentityRole(RoleNames.Admin)
        {
            Id = AdminRoleId,
            NormalizedName = RoleNames.Admin.ToUpperInvariant(),
            ConcurrencyStamp = "ADMIN-CONCURRENCY-STAMP-2024"
        },
        new IdentityRole(RoleNames.Seller)
        {
            Id = SellerRoleId,
            NormalizedName = RoleNames.Seller.ToUpperInvariant(),
            ConcurrencyStamp = "SELLER-CONCURRENCY-STAMP-2024"
        },
        new IdentityRole(RoleNames.User)
        {
            Id = UserRoleId,
            NormalizedName = RoleNames.User.ToUpperInvariant(),
            ConcurrencyStamp = "USER-CONCURRENCY-STAMP-2024"
        },
        new IdentityRole(RoleNames.Author)
        {
            Id = AuthorRoleId,
            NormalizedName = RoleNames.Author.ToUpperInvariant(),
            ConcurrencyStamp = "AUTHOR-CONCURRENCY-STAMP-2024"
        }
    };

    internal static readonly Guid AnalyticalThinkingId = Guid.Parse("d14d15d9-9111-4e84-8636-1f85f254dbe2");
    internal static readonly Guid CreativeVisionId = Guid.Parse("714bcd44-5450-4d8f-b1c6-2d03f509cf25");
    internal static readonly Guid LeadershipInfluenceId = Guid.Parse("62c35312-1687-4b72-9392-763d847f0304");
    internal static readonly Guid TeamCollaborationId = Guid.Parse("6443af72-ff8c-455c-9d5d-3899811df0a9");
    internal static readonly Guid EmotionalIntelligenceId = Guid.Parse("14f8367f-5a2b-43e8-ac26-9d22540194f8");
    internal static readonly Guid StrategicPlanningId = Guid.Parse("b7e55141-93e9-4bf9-939f-e9b7d970474d");
    internal static readonly Guid ProblemSolvingId = Guid.Parse("a65750ff-52c8-4ea0-bcb3-2e755a6b7f1a");
    internal static readonly Guid AdaptabilityId = Guid.Parse("86bce602-6888-4556-9147-bd5862828a95");
    internal static readonly Guid CommunicationExcellenceId = Guid.Parse("9057bb0c-8990-4afa-8b9a-0db8278aef07");
    internal static readonly Guid DecisionMakingId = Guid.Parse("04df80fb-f354-4c63-a9cf-5b2a2b31f06e");
    internal static readonly Guid InnovationCatalystId = Guid.Parse("1b6bcc04-8d16-42f5-8cbd-52e6946d934b");
    internal static readonly Guid ResilienceId = Guid.Parse("31246848-d036-45dc-a309-0d11a6ea625e");
    internal static readonly Guid LearningAgilityId = Guid.Parse("8c9f49b3-9b97-45fd-91d0-29b829409e55");
    internal static readonly Guid TimeManagementId = Guid.Parse("4c1f5f8c-c9fa-4a80-a439-3300d7b6d1d3");
    internal static readonly Guid CustomerFocusId = Guid.Parse("2df8dd3b-35cc-4d81-a879-89fa9da6c4c4");
    internal static readonly Guid TechnicalMasteryId = Guid.Parse("19a4d0f3-8f1c-4fa7-8cc3-b51d8438bc12");
    internal static readonly Guid MentoringId = Guid.Parse("76d03713-57c8-43ff-974c-4367bd5972b0");
    internal static readonly Guid NegotiationId = Guid.Parse("b0ab91f4-1f68-4f66-8d66-4d48e1e5e673");
    internal static readonly Guid ConflictResolutionId = Guid.Parse("09ce25b6-5ceb-4d18-8d63-4b4e9b17a94f");
    internal static readonly Guid EntrepreneurshipId = Guid.Parse("6ce515fb-6cdb-4a65-98b6-2d18c4ef82bb");
    internal static readonly Guid AttentionToDetailId = Guid.Parse("9cfca19a-3aeb-4ab8-8c08-0f09d4d8198f");
    internal static readonly Guid ProductivityDriveId = Guid.Parse("dfc84159-295a-4e86-b3d4-503340e9232b");
    internal static readonly Guid CulturalAwarenessId = Guid.Parse("3e5d9af2-d29d-4b04-91eb-6bc460f1fb07");
    internal static readonly Guid ServiceOrientationId = Guid.Parse("f8a2f94e-f857-4a49-b98f-0c04814e6a1b");

    internal static readonly IReadOnlyCollection<object> Talents = new[]
    {
        CreateTalent(AnalyticalThinkingId, "Analytical Thinking", "Breaks complex problems into clear insights backed by data."),
        CreateTalent(CreativeVisionId, "Creative Vision", "Imagines bold possibilities and turns them into tangible concepts."),
        CreateTalent(LeadershipInfluenceId, "Leadership Influence", "Inspires trust and guides teams toward a shared direction."),
        CreateTalent(TeamCollaborationId, "Team Collaboration", "Builds synergy by aligning individual strengths around collective goals."),
        CreateTalent(EmotionalIntelligenceId, "Emotional Intelligence", "Understands emotions and responds with empathy and composure."),
        CreateTalent(StrategicPlanningId, "Strategic Planning", "Connects long-term vision with actionable milestones."),
        CreateTalent(ProblemSolvingId, "Problem Solving", "Responds to obstacles quickly with practical solutions."),
        CreateTalent(AdaptabilityId, "Adaptability", "Thrives in change by staying flexible and resourceful."),
        CreateTalent(CommunicationExcellenceId, "Communication Excellence", "Shares ideas clearly and listens actively to ensure alignment."),
        CreateTalent(DecisionMakingId, "Decision Making", "Chooses the best path using balanced intuition and evidence."),
        CreateTalent(InnovationCatalystId, "Innovation Catalyst", "Champions experimentation that leads to new value."),
        CreateTalent(ResilienceId, "Resilience", "Maintains focus and optimism during demanding periods."),
        CreateTalent(LearningAgilityId, "Learning Agility", "Absorbs new knowledge rapidly and applies it effectively."),
        CreateTalent(TimeManagementId, "Time Management", "Prioritises commitments to deliver work predictably."),
        CreateTalent(CustomerFocusId, "Customer Focus", "Anticipates customer needs and responds with value."),
        CreateTalent(TechnicalMasteryId, "Technical Mastery", "Builds robust solutions by understanding core technologies."),
        CreateTalent(MentoringId, "Mentoring", "Elevates others by transferring knowledge and confidence."),
        CreateTalent(NegotiationId, "Negotiation", "Finds balanced agreements that respect all stakeholders."),
        CreateTalent(ConflictResolutionId, "Conflict Resolution", "Defuses tension and rebuilds collaboration with fairness."),
        CreateTalent(EntrepreneurshipId, "Entrepreneurship", "Spots growth opportunities and mobilises resources quickly."),
        CreateTalent(AttentionToDetailId, "Attention to Detail", "Delivers precise outcomes by spotting hidden variances."),
        CreateTalent(ProductivityDriveId, "Productivity Drive", "Keeps momentum high and removes obstacles to progress."),
        CreateTalent(CulturalAwarenessId, "Cultural Awareness", "Respects diversity and adapts communication across cultures."),
        CreateTalent(ServiceOrientationId, "Service Orientation", "Creates supportive experiences that strengthen relationships.")
    };

    internal static readonly IReadOnlyCollection<object> Questions = new[]
    {
        CreateQuestion(
            Guid.Parse("b8dfc7db-7037-46e0-bd87-2a56038e9e94"),
            "I enjoy examining data to uncover patterns that inform better choices.",
            AnalyticalThinkingId,
            ProblemSolvingId),
        CreateQuestion(
            Guid.Parse("7ebcaa9b-1f7c-4ee4-bb84-86f8bd70f2de"),
            "Colleagues rely on me to unite different perspectives around a common goal.",
            LeadershipInfluenceId,
            TeamCollaborationId),
        CreateQuestion(
            Guid.Parse("1d6fd44a-1573-49e3-8ed8-c95b99604743"),
            "Unexpected changes rarely unsettle me; I adapt and keep moving forward.",
            AdaptabilityId,
            ResilienceId),
        CreateQuestion(
            Guid.Parse("68128f7d-4c24-465f-9798-40e7044f4385"),
            "I translate complex ideas into simple language that everyone understands.",
            CommunicationExcellenceId,
            AttentionToDetailId),
        CreateQuestion(
            Guid.Parse("0dd45a36-6ba5-44a5-8c81-fcbad1ad167d"),
            "Long-term objectives guide the way I plan day-to-day activities.",
            StrategicPlanningId,
            TimeManagementId),
        CreateQuestion(
            Guid.Parse("9f67dc4e-9732-4f16-8601-085ae77f9184"),
            "I look for ways to delight customers even when it requires extra effort.",
            CustomerFocusId,
            ServiceOrientationId),
        CreateQuestion(
            Guid.Parse("f23cb49d-43bf-40e6-9af3-c0aac7714966"),
            "Sharing knowledge and coaching others energises me.",
            MentoringId,
            LeadershipInfluenceId),
        CreateQuestion(
            Guid.Parse("5e26de3e-9363-4b73-b8cd-8f335fd4dc86"),
            "When conflicts arise, I help the parties reach a respectful compromise.",
            ConflictResolutionId,
            NegotiationId),
        CreateQuestion(
            Guid.Parse("bf0ed5f5-6620-4f50-a9cf-b8cb328a4add"),
            "I rapidly learn new tools or concepts and apply them to improve results.",
            LearningAgilityId,
            TechnicalMasteryId),
        CreateQuestion(
            Guid.Parse("f1b8566e-0f64-40a7-b23f-0c1252d751a3"),
            "I often spot opportunities that others miss and rally support to pursue them.",
            EntrepreneurshipId,
            InnovationCatalystId),
        CreateQuestion(
            Guid.Parse("07c4f73f-fdf8-4d63-9a08-efeaad91e2be"),
            "Producing reliable work requires me to double-check the fine details.",
            AttentionToDetailId,
            ProductivityDriveId),
        CreateQuestion(
            Guid.Parse("60097457-1cf7-46c4-9782-754c2407ef12"),
            "I build rapport quickly with people from different cultures or backgrounds.",
            CulturalAwarenessId,
            EmotionalIntelligenceId)
    };

    private static object CreateTalent(Guid id, string name, string description)
        => new
        {
            Id = id,
            Name = name,
            Description = description,
            CreatorId = SystemUsers.AutomationId,
            UpdaterId = (string?)null,
            CreateDate = DefaultAuditTimestamp,
            UpdateDate = DefaultAuditTimestamp,
            RemoveDate = (DateTimeOffset?)null,
            IsDeleted = false,
            Ip = SeedIpAddress
        };

    private static object CreateQuestion(Guid id, string text, params Guid[] talentIds)
        => new
        {
            Id = id,
            Text = text,
            DefaultScale = LikertScale.Neutral,
            _talentIds = (talentIds ?? Array.Empty<Guid>()).ToList(),
            CreatorId = SystemUsers.AutomationId,
            UpdaterId = (string?)null,
            CreateDate = DefaultAuditTimestamp,
            UpdateDate = DefaultAuditTimestamp,
            RemoveDate = (DateTimeOffset?)null,
            IsDeleted = false,
            Ip = SeedIpAddress
        };
}
