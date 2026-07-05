GO

DECLARE @TemplateCode NVARCHAR(100) = N'SPM-NEW';
DECLARE @TemplateName NVARCHAR(200) = N'Confirmation Letter for Exemption (SPM)';
DECLARE @TemplateDescription NVARCHAR(1000) = N'Pilot applicant submission template for SPM using a hybrid shell with system and dynamic sections.';

DECLARE @PublishedTemplateStatusId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationTemplateStatuses
    WHERE Code = N'Published' AND IsDeleted = 0
);

DECLARE @PublishedFormStatusId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormDefinitionStatuses
    WHERE Code = N'Published' AND IsDeleted = 0
);

DECLARE @SystemFormSectionTypeId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationSectionTypes
    WHERE Code = N'SystemForm' AND IsDeleted = 0
);

DECLARE @DynamicFormSectionTypeId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationSectionTypes
    WHERE Code = N'DynamicForm' AND IsDeleted = 0
);

DECLARE @DocumentUploadSectionTypeId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationSectionTypes
    WHERE Code = N'DocumentUpload' AND IsDeleted = 0
);

DECLARE @ReviewSubmitSectionTypeId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationSectionTypes
    WHERE Code = N'ReviewSubmit' AND IsDeleted = 0
);

DECLARE @FieldTypeTextId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'Text' AND IsDeleted = 0
);

DECLARE @FieldTypeTextAreaId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'TextArea' AND IsDeleted = 0
);

DECLARE @FieldTypeNumberId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'Number' AND IsDeleted = 0
);

DECLARE @FieldTypeDateId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'Date' AND IsDeleted = 0
);

DECLARE @FieldTypeDropdownId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'Dropdown' AND IsDeleted = 0
);

DECLARE @FieldTypeCheckboxId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldTypes
    WHERE Code = N'Checkbox' AND IsDeleted = 0
);

DECLARE @ApplicationCategoryId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationCategories
    WHERE IsDeleted = 0
      AND
      (
          CodeKey = N'spm'
          OR Code = N'SPM'
          OR Name LIKE N'%SPM%'
      )
    ORDER BY SortOrder, Name
);

DECLARE @ApplicationForId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationFors
    WHERE IsDeleted = 0
      AND
      (
          Name LIKE N'%exemption%'
          OR Name LIKE N'%SPM%'
      )
    ORDER BY SortOrder, Name
);

DECLARE @ApplicationTypeId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationTypes
    WHERE IsDeleted = 0
      AND
      (
          Name LIKE N'%confirmation%'
          OR Name LIKE N'%exemption%'
          OR Name LIKE N'%SPM%'
      )
    ORDER BY SortOrder, Name
);

DECLARE @DraftApplicationStatusId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationStatuses
    WHERE IsDeleted = 0
      AND
      (
          CodeKey = N'draft'
          OR Name = N'Draft'
          OR Name LIKE N'%draft%'
      )
    ORDER BY SortOrder, Name
);

DECLARE @ProjectDetailsFormDefinitionId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormDefinitions
    WHERE Code = N'SPM-PROJECT-DETAILS'
      AND IsDeleted = 0
);

IF @ProjectDetailsFormDefinitionId IS NULL
BEGIN
    SET @ProjectDetailsFormDefinitionId = NEWID();

    INSERT INTO dbo.FormDefinitions
    (
        Id,
        Code,
        Name,
        Description,
        FormPurpose,
        IsSystem,
        IsActive
    )
    VALUES
    (
        @ProjectDetailsFormDefinitionId,
        N'SPM-PROJECT-DETAILS',
        N'SPM Project Details',
        N'Dynamic data-capture form for pilot SPM project details.',
        N'Section',
        0,
        1
    );
END;

DECLARE @ProjectDetailsFormVersionId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormDefinitionVersions
    WHERE FormDefinitionId = @ProjectDetailsFormDefinitionId
      AND VersionNumber = 1
      AND IsDeleted = 0
);

IF @ProjectDetailsFormVersionId IS NULL
BEGIN
    SET @ProjectDetailsFormVersionId = NEWID();

    INSERT INTO dbo.FormDefinitionVersions
    (
        Id,
        FormDefinitionId,
        VersionNumber,
        FormDefinitionStatusId,
        VersionName,
        Description,
        LayoutJson,
        ValidationJson,
        PublishedAt
    )
    VALUES
    (
        @ProjectDetailsFormVersionId,
        @ProjectDetailsFormDefinitionId,
        1,
        @PublishedFormStatusId,
        N'v1',
        N'Pilot published version for SPM project details.',
        N'{"layout":"single-column","groups":[{"code":"project-details","title":"Project Details"}]}',
        N'{"mode":"section"}',
        SYSUTCDATETIME()
    );
END;

UPDATE dbo.FormDefinitions
SET CurrentPublishedVersionId = @ProjectDetailsFormVersionId,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @ProjectDetailsFormDefinitionId
  AND (CurrentPublishedVersionId IS NULL OR CurrentPublishedVersionId <> @ProjectDetailsFormVersionId);

DECLARE @FieldProjectTitleId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'project_title'
      AND IsDeleted = 0
);

IF @FieldProjectTitleId IS NULL
BEGIN
    SET @FieldProjectTitleId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        Placeholder,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired
    )
    VALUES
    (
        @FieldProjectTitleId,
        @ProjectDetailsFormVersionId,
        N'project_title',
        N'Project Title',
        N'Enter project title',
        N'Business-defined title for the exemption request.',
        @FieldTypeTextId,
        10,
        1
    );
END;

DECLARE @FieldProjectDescriptionId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'project_description'
      AND IsDeleted = 0
);

IF @FieldProjectDescriptionId IS NULL
BEGIN
    SET @FieldProjectDescriptionId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        Placeholder,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired
    )
    VALUES
    (
        @FieldProjectDescriptionId,
        @ProjectDetailsFormVersionId,
        N'project_description',
        N'Project Description',
        N'Summarise the project',
        N'Brief narrative of the project and exemption rationale.',
        @FieldTypeTextAreaId,
        20,
        1
    );
END;

DECLARE @FieldExpectedInvestmentId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'expected_investment_amount'
      AND IsDeleted = 0
);

IF @FieldExpectedInvestmentId IS NULL
BEGIN
    SET @FieldExpectedInvestmentId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        Placeholder,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired,
        ValidationJson
    )
    VALUES
    (
        @FieldExpectedInvestmentId,
        @ProjectDetailsFormVersionId,
        N'expected_investment_amount',
        N'Expected Investment Amount (RM)',
        N'0.00',
        N'Estimated investment amount for the project.',
        @FieldTypeNumberId,
        30,
        0,
        N'{"min":0,"scale":2}'
    );
END;

DECLARE @FieldImplementationDateId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'expected_implementation_date'
      AND IsDeleted = 0
);

IF @FieldImplementationDateId IS NULL
BEGIN
    SET @FieldImplementationDateId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired
    )
    VALUES
    (
        @FieldImplementationDateId,
        @ProjectDetailsFormVersionId,
        N'expected_implementation_date',
        N'Expected Implementation Date',
        N'Estimated start date for the project implementation.',
        @FieldTypeDateId,
        40,
        0
    );
END;

DECLARE @FieldSectorId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'project_sector'
      AND IsDeleted = 0
);

IF @FieldSectorId IS NULL
BEGIN
    SET @FieldSectorId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired
    )
    VALUES
    (
        @FieldSectorId,
        @ProjectDetailsFormVersionId,
        N'project_sector',
        N'Project Sector',
        N'Choose the sector most relevant to the project.',
        @FieldTypeDropdownId,
        50,
        1
    );
END;

MERGE dbo.FormFieldOptions AS target
USING
(
    VALUES
        (@FieldSectorId, N'manufacturing', N'Manufacturing', 10),
        (@FieldSectorId, N'services', N'Services', 20),
        (@FieldSectorId, N'logistics', N'Logistics', 30),
        (@FieldSectorId, N'other', N'Other', 40)
) AS source (FormFieldDefinitionId, OptionValue, OptionLabel, DisplayOrder)
    ON target.FormFieldDefinitionId = source.FormFieldDefinitionId
   AND target.OptionValue = source.OptionValue
WHEN MATCHED THEN
    UPDATE SET
        target.OptionLabel = source.OptionLabel,
        target.DisplayOrder = source.DisplayOrder,
        target.IsActive = 1,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        FormFieldDefinitionId,
        OptionValue,
        OptionLabel,
        DisplayOrder,
        IsDefault,
        IsActive
    )
    VALUES
    (
        source.FormFieldDefinitionId,
        source.OptionValue,
        source.OptionLabel,
        source.DisplayOrder,
        0,
        1
    );
;

DECLARE @FieldDeclarationId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.FormFieldDefinitions
    WHERE FormDefinitionVersionId = @ProjectDetailsFormVersionId
      AND FieldCode = N'is_information_true'
      AND IsDeleted = 0
);

IF @FieldDeclarationId IS NULL
BEGIN
    SET @FieldDeclarationId = NEWID();

    INSERT INTO dbo.FormFieldDefinitions
    (
        Id,
        FormDefinitionVersionId,
        FieldCode,
        Label,
        HelpText,
        FormFieldTypeId,
        DisplayOrder,
        IsRequired
    )
    VALUES
    (
        @FieldDeclarationId,
        @ProjectDetailsFormVersionId,
        N'is_information_true',
        N'I confirm that the information provided is true and complete.',
        N'Required declaration before applicant submission.',
        @FieldTypeCheckboxId,
        60,
        1
    );
END;

DECLARE @TemplateId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationTemplates
    WHERE Code = @TemplateCode
      AND IsDeleted = 0
);

IF @TemplateId IS NULL
BEGIN
    SET @TemplateId = NEWID();

    INSERT INTO dbo.ApplicationTemplates
    (
        Id,
        Code,
        Name,
        Description,
        ApplicationCategoryId,
        ApplicationForId,
        ApplicationTypeId,
        DefaultApplicationStatusId,
        IsActive
    )
    VALUES
    (
        @TemplateId,
        @TemplateCode,
        @TemplateName,
        @TemplateDescription,
        @ApplicationCategoryId,
        @ApplicationForId,
        @ApplicationTypeId,
        @DraftApplicationStatusId,
        1
    );
END
ELSE
BEGIN
    UPDATE dbo.ApplicationTemplates
    SET Name = @TemplateName,
        Description = @TemplateDescription,
        ApplicationCategoryId = COALESCE(@ApplicationCategoryId, ApplicationCategoryId),
        ApplicationForId = COALESCE(@ApplicationForId, ApplicationForId),
        ApplicationTypeId = COALESCE(@ApplicationTypeId, ApplicationTypeId),
        DefaultApplicationStatusId = COALESCE(@DraftApplicationStatusId, DefaultApplicationStatusId),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @TemplateId;
END;

DECLARE @TemplateVersionId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.ApplicationTemplateVersions
    WHERE ApplicationTemplateId = @TemplateId
      AND VersionNumber = 1
      AND IsDeleted = 0
);

IF @TemplateVersionId IS NULL
BEGIN
    SET @TemplateVersionId = NEWID();

    INSERT INTO dbo.ApplicationTemplateVersions
    (
        Id,
        ApplicationTemplateId,
        VersionNumber,
        ApplicationTemplateStatusId,
        VersionName,
        Description,
        EffectiveFrom,
        PublishedAt,
        SubmissionConfigJson
    )
    VALUES
    (
        @TemplateVersionId,
        @TemplateId,
        1,
        @PublishedTemplateStatusId,
        N'v1',
        N'Pilot published template for SPM applicant submission.',
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        N'{"code":"SPM-NEW","layout":"left-nav-shell","saveMode":"draft"}'
    );
END;

UPDATE dbo.ApplicationTemplates
SET CurrentPublishedVersionId = @TemplateVersionId,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @TemplateId
  AND (CurrentPublishedVersionId IS NULL OR CurrentPublishedVersionId <> @TemplateVersionId);

MERGE dbo.ApplicationTemplateSections AS target
USING
(
    VALUES
        (N'SECTION_A', N'Section A: Particular of Company', 10, @SystemFormSectionTypeId, N'/irpm/company-profile', N'ParticularOfCompany', CAST(NULL AS UNIQUEIDENTIFIER), N'building-2', 1, 1, 0, N'OnSave', N'{"source":"company-profile"}'),
        (N'SECTION_B', N'Section B: Project Information', 20, @DynamicFormSectionTypeId, CAST(NULL AS NVARCHAR(200)), CAST(NULL AS NVARCHAR(200)), @ProjectDetailsFormVersionId, N'clipboard-text', 1, 1, 0, N'OnSave', N'{"source":"dynamic-form"}'),
        (N'SECTION_C', N'Section C: Attachments', 30, @DocumentUploadSectionTypeId, N'/applications/attachments', N'ApplicationAttachments', CAST(NULL AS UNIQUEIDENTIFIER), N'paperclip', 1, 1, 0, N'OnSubmit', N'{"allowed":["pdf","docx","xlsx"]}'),
        (N'SECTION_D', N'Section D: Review & Submit', 40, @ReviewSubmitSectionTypeId, N'/applications/review', N'ApplicationReviewSubmit', CAST(NULL AS UNIQUEIDENTIFIER), N'check-circle', 1, 1, 0, N'OnSubmit', N'{"summary":true}')
) AS source
(
    SectionCode,
    Title,
    DisplayOrder,
    ApplicationSectionTypeId,
    SystemRouteKey,
    SystemComponentKey,
    FormDefinitionVersionId,
    StepIcon,
    IsVisible,
    IsRequired,
    CanRepeat,
    ValidationMode,
    SectionSettingsJson
)
    ON target.ApplicationTemplateVersionId = @TemplateVersionId
   AND target.SectionCode = source.SectionCode
WHEN MATCHED THEN
    UPDATE SET
        target.Title = source.Title,
        target.DisplayOrder = source.DisplayOrder,
        target.ApplicationSectionTypeId = source.ApplicationSectionTypeId,
        target.SystemRouteKey = source.SystemRouteKey,
        target.SystemComponentKey = source.SystemComponentKey,
        target.FormDefinitionVersionId = source.FormDefinitionVersionId,
        target.StepIcon = source.StepIcon,
        target.IsVisible = source.IsVisible,
        target.IsRequired = source.IsRequired,
        target.CanRepeat = source.CanRepeat,
        target.ValidationMode = source.ValidationMode,
        target.SectionSettingsJson = source.SectionSettingsJson,
        target.UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        ApplicationTemplateVersionId,
        SectionCode,
        Title,
        DisplayOrder,
        ApplicationSectionTypeId,
        SystemRouteKey,
        SystemComponentKey,
        FormDefinitionVersionId,
        StepIcon,
        IsVisible,
        IsRequired,
        CanRepeat,
        ValidationMode,
        SectionSettingsJson
    )
    VALUES
    (
        @TemplateVersionId,
        source.SectionCode,
        source.Title,
        source.DisplayOrder,
        source.ApplicationSectionTypeId,
        source.SystemRouteKey,
        source.SystemComponentKey,
        source.FormDefinitionVersionId,
        source.StepIcon,
        source.IsVisible,
        source.IsRequired,
        source.CanRepeat,
        source.ValidationMode,
        source.SectionSettingsJson
    );
;

SELECT
    TemplateCode = @TemplateCode,
    TemplateId = @TemplateId,
    TemplateVersionId = @TemplateVersionId,
    ProjectDetailsFormDefinitionId = @ProjectDetailsFormDefinitionId,
    ProjectDetailsFormVersionId = @ProjectDetailsFormVersionId,
    ApplicationCategoryId = @ApplicationCategoryId,
    ApplicationForId = @ApplicationForId,
    ApplicationTypeId = @ApplicationTypeId,
    DefaultApplicationStatusId = @DraftApplicationStatusId;
GO
