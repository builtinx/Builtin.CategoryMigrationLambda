namespace CategoryMigrationLambda.Services;

/// <summary>
/// Embedded category mappings for migration (copied from FilterService to avoid dependencies)
/// </summary>
public static class CategoryMappings
{
    public static readonly IReadOnlyDictionary<(int? LegacyCategoryId, int? LegacySubcategoryId), (int? NewCategoryId, int? NewSubcategoryId)> CategoryMappingRulesByIds = new Dictionary<(int? LegacyCategoryId, int? LegacySubcategoryId), (int? NewCategoryId, int? NewSubcategoryId)>
    {
        // Category-only mappings (no subcategory) - ordered to match source of truth
        { (390, null), (2, null) }, // Customer Success -> Customer Success + Experience
        { (391, null), (3, null) }, // Cybersecurity + IT -> Cybersecurity
        { (147, null), (4, null) }, // Data + Analytics -> Data & Analytics
        { (148, null), (6, null) }, // Design + UX -> Design
        { (149, null), (8, null) }, // Developer + Engineer -> Engineering
        { (146, null), (9, null) }, // Finance -> Finance
        { (150, null), (11, null) }, // HR + Recruiting -> HR + Recruiting
        { (152, null), (12, null) }, // Legal -> Legal
        { (153, null), (14, null) }, // Marketing -> Marketing
        { (154, null), (15, null) }, // Operations -> Operations + Support
        { (155, null), (16, null) }, // Product -> Product Management
        { (156, null), (17, null) }, // Project Mgmt -> Program and Project Management
        { (157, null), (19, null) }, // Sales -> Sales
        { (158, null), (14, null) }, // Content -> Marketing

        // Null mappings for Management, Other, and Internships
        { (151, null), (null, null) }, // Internships -> null

        // Data + Analytics mappings (4, 5)
        { (147, 508), (4, 38) }, // Data + Analytics, Analytics -> Data & Analytics, Reporting & Insights
        { (147, 509), (4, 38) }, // Data + Analytics, Analysis & Reporting -> Data & Analytics, Reporting & Insights
        { (147, 201), (4, 36) }, // Data + Analytics, Business Intelligence -> Data & Analytics, Business Intelligence (BI)
        { (147, 510), (4, 36) }, // Data + Analytics, Business Intelligence -> Data & Analytics, Business Intelligence (BI)
        { (147, 511), (4, 35) }, // Data + Analytics, Data Engineering -> Data & Analytics, Data Engineering
        { (147, 512), (5, 42) }, // Data + Analytics, Data Science -> AI & Machine Learning, Data Science
        { (147, 513), (5, 41) }, // Data + Analytics, Machine Learning -> AI & Machine Learning, Machine Learning Engineer

        // Developer + Engineer mappings (8, 19)
        { (149, 516), (8, 58) }, // Developer + Engineer, Android (Java) -> Engineering, Software Engineering
        { (149, 517), (8, 58) }, // Developer + Engineer, C++ -> Engineering, Software Engineering
        { (149, 518), (8, 58) }, // Developer + Engineer, C# -> Engineering, Software Engineering
        { (149, 519), (8, 58) }, // Developer + Engineer, DevOps -> Engineering, Software Engineering
        { (149, 520), (8, 58) }, // Developer + Engineer, Front-End -> Engineering, Software Engineering
        { (149, 521), (8, 58) }, // Developer + Engineer, Golang -> Engineering, Software Engineering
        { (149, 522), (8, 58) }, // Developer + Engineer, Java -> Engineering, Software Engineering
        { (149, 523), (8, 58) }, // Developer + Engineer, Javascript -> Engineering, Software Engineering
        { (149, 524), (8, 62) }, // Developer + Engineer, Hardware -> Engineering, Hardware Engineering
        { (149, 525), (8, 58) }, // Developer + Engineer, iOS (Objective-C) -> Engineering, Software Engineering
        { (149, 526), (8, 58) }, // Developer + Engineer, Linux -> Engineering, Software Engineering
        { (149, 527), (null, null) }, // Developer + Engineer, Management -> null
        { (149, 528), (8, 58) }, // Developer + Engineer, .NET -> Engineering, Software Engineering
        { (149, 529), (8, 58) }, // Developer + Engineer, Perl -> Engineering, Software Engineering
        { (149, 530), (8, 58) }, // Developer + Engineer, PHP -> Engineering, Software Engineering
        { (149, 531), (8, 58) }, // Developer + Engineer, Python -> Engineering, Software Engineering
        { (149, 532), (8, 60) }, // Developer + Engineer, QA -> Engineering, QA/Test Engineering
        { (149, 533), (8, 58) }, // Developer + Engineer, Ruby -> Engineering, Software Engineering
        { (149, 534), (8, 58) }, // Developer + Engineer, Salesforce -> Engineering, Software Engineering
        { (149, 535), (19, 126) }, // Developer + Engineer, Sales Engineer -> Sales, Sales Engineer
        { (149, 536), (8, 58) }, // Developer + Engineer, Scala -> Engineering, Software Engineering

        // Cybersecurity + IT mappings (3, 15)
        { (391, 537), (3, 29) }, // Cybersecurity + IT, Security -> Cybersecurity, Security Operations
        { (391, 541), (15, 104) }, // Cybersecurity + IT, IT -> Operations + Support, IT Support + Helpdesk
        { (391, 544), (15, 104) }, // Cybersecurity + IT, Technical Support -> Operations + Support, IT Support + Helpdesk

        // Operations mappings (15)
        { (154, 542), (15, 105) }, // Operations, Office Management -> Operations + Support, Office Management
        { (154, 543), (15, 106) }, // Operations, Operations Management -> Operations + Support, Strategic Operations

        // Sales mappings (19)
        { (157, 454), (19, 121) }, // Sales, Account Development -> Sales, Account Executive
        { (157, 455), (19, 122) }, // Sales, Account Management -> Sales, Account Management
        { (157, 465), (19, 123) }, // Sales, Sales Management -> Sales, Leadership
        { (157, 466), (19, 124) }, // Sales, Sales Operations -> Sales, Sales Operations
        { (157, 462), (19, 125) }, // Sales, Inside Sales -> Sales, Sales Development
        { (157, 535), (19, 126) }, // Sales, Sales Engineer -> Sales, Sales Engineer

        // Add more mappings as needed...
        // Note: This is a subset of the full mappings for brevity
        // In production, include all mappings from the FilterService
    };
}
