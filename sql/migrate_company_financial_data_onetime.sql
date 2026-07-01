SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#CompanyMap') IS NOT NULL DROP TABLE #CompanyMap;
IF OBJECT_ID('tempdb..#CountryMap') IS NOT NULL DROP TABLE #CountryMap;
IF OBJECT_ID('tempdb..#FinancialDetailsSource') IS NOT NULL DROP TABLE #FinancialDetailsSource;
IF OBJECT_ID('tempdb..#FinancialDetailMap') IS NOT NULL DROP TABLE #FinancialDetailMap;
IF OBJECT_ID('tempdb..#CompanyMalaysiaMap') IS NOT NULL DROP TABLE #CompanyMalaysiaMap;
IF OBJECT_ID('tempdb..#CompanyIncorporatedMap') IS NOT NULL DROP TABLE #CompanyIncorporatedMap;
IF OBJECT_ID('tempdb..#ForeignCompanyMap') IS NOT NULL DROP TABLE #ForeignCompanyMap;

SELECT MigratedId, Id AS CompanyProfileId
INTO #CompanyMap
FROM dbo.CompanyProfiles
WHERE MigratedId IS NOT NULL;

SELECT MigratedId, Id AS CountryId
INTO #CountryMap
FROM dbo.LookupCountries
WHERE MigratedId IS NOT NULL;

DELETE FROM dbo.CompanyProfileUltimateParentHoldingCompanies;
DELETE FROM dbo.CompanyProfileOtherSources;
DELETE FROM dbo.CompanyProfileTotalFinancings;
DELETE FROM dbo.CompanyProfileLoanForeigns;
DELETE FROM dbo.CompanyProfileLoanDomestics;
DELETE FROM dbo.CompanyProfileLoans;
DELETE FROM dbo.CompanyProfileCompanyIncorporatedCountries;
DELETE FROM dbo.CompanyProfileCompanyIncorporated;
DELETE FROM dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia;
DELETE FROM dbo.CompanyProfilePaidUpCapitalForeignCompanies;
DELETE FROM dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals;
DELETE FROM dbo.CompanyProfilePaidUpCapitals;
DELETE FROM dbo.CompanyProfileFinancialPerformanceRecords;
DELETE FROM dbo.CompanyProfileEquityStructures;
DELETE FROM dbo.CompanyProfileAuthorizedCapitals;
DELETE FROM dbo.CompanyProfileFinancialDetails;

SELECT
    src.MigratedId,
    src.LegacyProjectId,
    src.FinancialYear,
    src.EffectiveDate,
    src.ProjectStatusId,
    src.SourceCreatedByLegacyUserId,
    src.SourceCreatedAt,
    src.SourceUpdatedByLegacyUserId,
    src.SourceUpdatedAt,
    map.CompanyProfileId
INTO #FinancialDetailsSource
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        '
        SELECT
            pf.ID AS MigratedId,
            pf.PROJECTID AS LegacyProjectId,
            pf.[YEAR] AS FinancialYear,
            pf.EFFECTIVEDATE AS EffectiveDate,
            pf.PROJECTSTATUSID AS ProjectStatusId,
            pf.CREATEDBY AS SourceCreatedByLegacyUserId,
            pf.CREATEDDATETIME AS SourceCreatedAt,
            pf.MODIFIEDBY AS SourceUpdatedByLegacyUserId,
            pf.MODIFIEDDATETIME AS SourceUpdatedAt,
            COALESCE(fs.COMPANYID, project.COMPANYID) AS LegacyCompanyId
        FROM Outsystems.dbo.OSUSR_LPP_PROJECTFINANCING pf
        LEFT JOIN Outsystems.dbo.OSUSR_LPP_PROJECT project ON project.ID = pf.PROJECTID
        LEFT JOIN Outsystems.dbo.OSUSR_LPP_FINANCINGSTRUCTURE fs ON fs.ID = pf.ID
        WHERE COALESCE(fs.COMPANYID, project.COMPANYID) IS NOT NULL
        '
    ) src
JOIN #CompanyMap map ON map.MigratedId = src.LegacyCompanyId;

INSERT INTO dbo.CompanyProfileFinancialDetails
(
    CompanyProfileId,
    MigratedId,
    LegacyProjectId,
    FinancialYear,
    EffectiveDate,
    ProjectStatusId,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    CompanyProfileId,
    MigratedId,
    LegacyProjectId,
    FinancialYear,
    EffectiveDate,
    ProjectStatusId,
    SourceCreatedByLegacyUserId,
    NULLIF(SourceCreatedAt, CONVERT(datetime, '1900-01-01')),
    SourceUpdatedByLegacyUserId,
    NULLIF(SourceUpdatedAt, CONVERT(datetime, '1900-01-01'))
FROM #FinancialDetailsSource;

SELECT MigratedId, Id AS FinancialDetailsId
INTO #FinancialDetailMap
FROM dbo.CompanyProfileFinancialDetails;

INSERT INTO dbo.CompanyProfileAuthorizedCapitals
(
    CompanyProfileId,
    MigratedCompanyId,
    AuthorizedCapital,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    map.CompanyProfileId,
    src.COMPANYID,
    src.AUTHORIZEDCAPITAL,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT COMPANYID, AUTHORIZEDCAPITAL, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_AUTHORIZEDCAPITAL1'
    ) src
JOIN #CompanyMap map ON map.MigratedId = src.COMPANYID;

INSERT INTO dbo.CompanyProfileEquityStructures
(
    FinancialDetailsId,
    BumiRm,
    BumiPercent,
    NonBumiRm,
    NonBumiPercent,
    ForeignRm,
    ForeignPercent,
    TotalRm,
    TotalPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.BUMI_RM,
    src.BUMI_PERCENT,
    src.NONBUMI_RM,
    src.NONBUMI_PERCENT,
    src.FOREIGN_RM,
    src.FOREIGN_PERCENT,
    src.TOTAL_RM,
    src.TOTAL_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT FINANCINGDETAILSID, BUMI_RM, BUMI_PERCENT, NONBUMI_RM, NONBUMI_PERCENT, FOREIGN_RM, FOREIGN_PERCENT, TOTAL_RM, TOTAL_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_EQUITYSTRUCTURE'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID;

INSERT INTO dbo.CompanyProfileFinancialPerformanceRecords
(
    FinancialDetailsId,
    Revenue,
    Profit,
    TaxableExpenditure,
    ExportSales,
    LocalSales,
    TotalAsset,
    ShareholderFund,
    EmployeeCount,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.REVENUE,
    src.PROFIT,
    src.TAXABLEEXPENDITURE,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT FINANCINGDETAILSID, REVENUE, PROFIT, TAXABLEEXPENDITURE, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_FINANCIALPERFORMANCERECORD'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID;

INSERT INTO dbo.CompanyProfilePaidUpCapitals
(
    FinancialDetailsId,
    TotalPaidUpCapital,
    TotalReserves,
    TotalShareholderFund,
    TotalRmMalaysianIndividuals,
    TotalPercentMalaysianIndividuals,
    TotalRmForeignCompany,
    TotalPercentForeignCompany,
    TotalRmCompanyMalaysia,
    TotalPercentCompanyMalaysia,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.TOTAL_PAIDUPCAPITAL,
    src.TOTAL_RESERVES,
    src.TOTAL_SHAREHOLDERFUND,
    src.TOTALRM_MSIANINDIVIDUALS,
    src.TOTALPERCENT_MSIANINDIVIDUAL,
    src.TOTALRM_FOREIGNCOMPANY,
    src.TOTALPERCENT_FOREIGNCOMPANY,
    src.TOTALRM_COMPANYMALAYSIA,
    src.TOTALPERCENT_COMPANYMALAYSI,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT FINANCINGDETAILSID, TOTAL_PAIDUPCAPITAL, TOTAL_RESERVES, TOTAL_SHAREHOLDERFUND, TOTALRM_MSIANINDIVIDUALS, TOTALPERCENT_MSIANINDIVIDUAL, TOTALRM_FOREIGNCOMPANY, TOTALPERCENT_FOREIGNCOMPANY, TOTALRM_COMPANYMALAYSIA, TOTALPERCENT_COMPANYMALAYSI, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_PUC_PAIDUPCAPITAL'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID;

INSERT INTO dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals
(
    FinancialDetailsId,
    BumiputeraRm,
    BumiputeraPercent,
    NonBumiputeraRm,
    NonBumiputeraPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.BUMIPUTERA_RM,
    src.BUMIPUTERA_PERCENT,
    src.NONBUMIPUTERA_RM,
    src.NONBUMIPUTERA_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT PUC_PAIDUPCAPITALID, BUMIPUTERA_RM, BUMIPUTERA_PERCENT, NONBUMIPUTERA_RM, NONBUMIPUTERA_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_PUC_MALAYSIANINDIVIDUALS'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.PUC_PAIDUPCAPITALID;

INSERT INTO dbo.CompanyProfilePaidUpCapitalForeignCompanies
(
    FinancialDetailsId,
    MigratedId,
    CompanyName,
    CountryId,
    LegacyCountryId,
    AmountRm,
    AmountPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    src.COMPANYNAME,
    c.CountryId,
    src.COUNTRYID,
    src.AMOUNT_RM,
    src.AMOUNT_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT PUC_PAIDUPCAPITALID, ID, COMPANYNAME, COUNTRYID, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_PUC_FOREIGNCOMPANY'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.PUC_PAIDUPCAPITALID
LEFT JOIN #CountryMap c ON c.MigratedId = src.COUNTRYID;

INSERT INTO dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia
(
    FinancialDetailsId,
    MigratedId,
    CompanyName,
    AmountRm,
    AmountPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    src.COMPANYNAME,
    src.AMOUNT_RM,
    src.AMOUNT_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT PUC_PAIDUPCAPITALID, ID, COMPANYNAME, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_PUC_COMPANYMALAYSIA'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.PUC_PAIDUPCAPITALID;

SELECT MigratedId, Id AS PaidUpCapitalCompanyMalaysiaEntryId
INTO #CompanyMalaysiaMap
FROM dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia;

INSERT INTO dbo.CompanyProfileCompanyIncorporated
(
    FinancialDetailsId,
    MigratedId,
    PaidUpCapitalCompanyMalaysiaEntryId,
    LocalCompanyTypeId,
    BumiPercent,
    NonBumiPercent,
    ForeignCountryId,
    LegacyForeignCountryId,
    ForeignPercent,
    TotalPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    cm.PaidUpCapitalCompanyMalaysiaEntryId,
    src.LOCALCOMPANYTYPEID,
    src.BUMI_PERCENT,
    src.NONBUMI_PERCENT,
    c.CountryId,
    src.FOREIGN_COUNTRYID,
    src.FOREIGN_PERCENT,
    src.TOTAL_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT ID, FINANCINGDETAILSID, PUC_COMPANYMALAYSIAID, LOCALCOMPANYTYPEID, BUMI_PERCENT, NONBUMI_PERCENT, FOREIGN_COUNTRYID, FOREIGN_PERCENT, TOTAL_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_COMPANYINCORPORATED'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID
JOIN #CompanyMalaysiaMap cm ON cm.MigratedId = src.PUC_COMPANYMALAYSIAID
LEFT JOIN #CountryMap c ON c.MigratedId = src.FOREIGN_COUNTRYID;

SELECT pcm.MigratedId, ci.Id AS CompanyIncorporatedEntryId
INTO #CompanyIncorporatedMap
FROM dbo.CompanyProfileCompanyIncorporated ci
JOIN dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia pcm ON pcm.Id = ci.PaidUpCapitalCompanyMalaysiaEntryId;

INSERT INTO dbo.CompanyProfileCompanyIncorporatedCountries
(
    CompanyIncorporatedEntryId,
    MigratedId,
    CountryId,
    LegacyCountryId,
    CountryPercent,
    AmountRm,
    PercentOverTotal
)
SELECT
    ci.CompanyIncorporatedEntryId,
    src.ID,
    c.CountryId,
    src.FOREIGN_COUNTRYID,
    src.COUNTRYPERCENT,
    src.AMOUNT_RM,
    src.PERCENTOVERTOTAL
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT ID, PUC_COMPANYMALAYSIAID, FOREIGN_COUNTRYID, COUNTRYPERCENT, AMOUNT_RM, PERCENTOVERTOTAL FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_COMPINCCOUNTRIES'
    ) src
JOIN #CompanyIncorporatedMap ci ON ci.MigratedId = src.PUC_COMPANYMALAYSIAID
LEFT JOIN #CountryMap c ON c.MigratedId = src.FOREIGN_COUNTRYID;

INSERT INTO dbo.CompanyProfileLoans
(
    FinancialDetailsId,
    TotalRmLoan,
    TotalLoanPercent,
    DomesticDescription,
    ForeignDescription,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.TOTALRM_LOAN,
    src.TOTALLOAN_PERCENT,
    src.DOMESTICDESCRIPTION,
    src.FOREIGNDESCRIPTION,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT FINANCINGDETAILSID, TOTALRM_LOAN, TOTALLOAN_PERCENT, DOMESTICDESCRIPTION, FOREIGNDESCRIPTION, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_LOAN'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID;

INSERT INTO dbo.CompanyProfileLoanDomestics
(
    FinancialDetailsId,
    AmountRm,
    AmountPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.AMOUNT_RM,
    src.AMOUNT_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT LOANID, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_LOAN_DOMESTIC'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.LOANID;

INSERT INTO dbo.CompanyProfileLoanForeigns
(
    FinancialDetailsId,
    MigratedId,
    CountryId,
    LegacyCountryId,
    AmountRm,
    AmountPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    c.CountryId,
    src.COUNTRYOFORIGIN,
    src.AMOUNT_RM,
    src.AMOUNT_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT LOANID, ID, COUNTRYOFORIGIN, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_LOAN_FOREIGN'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.LOANID
LEFT JOIN #CountryMap c ON c.MigratedId = src.COUNTRYOFORIGIN;

INSERT INTO dbo.CompanyProfileTotalFinancings
(
    FinancialDetailsId,
    TotalPaidUpCapital,
    TotalReserve,
    TotalLoan,
    TotalRmOtherSources,
    TotalPercentOtherSources,
    TotalFinancingRm,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.TOTALPAIDUPCAPITAL,
    src.TOTALRESERVE,
    src.TOTALLOAN,
    src.TOTALRM_OTHERSOURCES,
    src.TOTALPERCENT_OTHERSOURCES,
    src.TOTALFINANCING_RM,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT FINANCINGDETAILSID, TOTALPAIDUPCAPITAL, TOTALRESERVE, TOTALLOAN, TOTALRM_OTHERSOURCES, TOTALPERCENT_OTHERSOURCES, TOTALFINANCING_RM, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_TOTALFINANCING'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID;

INSERT INTO dbo.CompanyProfileOtherSources
(
    FinancialDetailsId,
    MigratedId,
    OtherSources,
    AmountRm,
    AmountPercent,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    src.OTHERSOURCES,
    src.AMOUNT_RM,
    src.AMOUNT_PERCENT,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT TOTALFINANCINGID, ID, OTHERSOURCES, AMOUNT_RM, AMOUNT_PERCENT, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_OTHERSOURCES'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.TOTALFINANCINGID;

SELECT MigratedId, Id AS PaidUpCapitalForeignCompanyEntryId
INTO #ForeignCompanyMap
FROM dbo.CompanyProfilePaidUpCapitalForeignCompanies;

INSERT INTO dbo.CompanyProfileUltimateParentHoldingCompanies
(
    FinancialDetailsId,
    MigratedId,
    PaidUpCapitalForeignCompanyEntryId,
    UltimateCompany,
    CountryId,
    LegacyCountryId,
    SourceCreatedByLegacyUserId,
    SourceCreatedAt,
    SourceUpdatedByLegacyUserId,
    SourceUpdatedAt
)
SELECT
    fd.FinancialDetailsId,
    src.ID,
    fc.PaidUpCapitalForeignCompanyEntryId,
    src.ULTIMATECOMPANY,
    c.CountryId,
    src.COUNTRYOFORIGIN,
    src.CREATEDBY,
    NULLIF(src.CREATEDDATETIME, CONVERT(datetime, '1900-01-01')),
    src.MODIFIEDBY,
    NULLIF(src.MODIFIEDDATETIME, CONVERT(datetime, '1900-01-01'))
FROM OPENROWSET(
        'MSOLEDBSQL',
        'Server=172.16.203.144;UID=sa;PWD=M1D@db@dM1N;TrustServerCertificate=Yes;',
        'SELECT ID, FINANCINGDETAILSID, PUC_FOREIGNCOMPANYID, ULTIMATECOMPANY, COUNTRYOFORIGIN, CREATEDBY, CREATEDDATETIME, MODIFIEDBY, MODIFIEDDATETIME FROM Outsystems.dbo.OSUSR_LPP_COMPFIN_ULTIMATEPARENTHOLDINGCOMPANY'
    ) src
JOIN #FinancialDetailMap fd ON fd.MigratedId = src.FINANCINGDETAILSID
JOIN #ForeignCompanyMap fc ON fc.MigratedId = src.PUC_FOREIGNCOMPANYID
LEFT JOIN #CountryMap c ON c.MigratedId = src.COUNTRYOFORIGIN;

SELECT 'CompanyProfileFinancialDetails' AS TableName, COUNT(*) AS [Rows] FROM dbo.CompanyProfileFinancialDetails
UNION ALL SELECT 'CompanyProfileAuthorizedCapitals', COUNT(*) FROM dbo.CompanyProfileAuthorizedCapitals
UNION ALL SELECT 'CompanyProfileEquityStructures', COUNT(*) FROM dbo.CompanyProfileEquityStructures
UNION ALL SELECT 'CompanyProfileFinancialPerformanceRecords', COUNT(*) FROM dbo.CompanyProfileFinancialPerformanceRecords
UNION ALL SELECT 'CompanyProfilePaidUpCapitals', COUNT(*) FROM dbo.CompanyProfilePaidUpCapitals
UNION ALL SELECT 'CompanyProfilePaidUpCapitalMalaysianIndividuals', COUNT(*) FROM dbo.CompanyProfilePaidUpCapitalMalaysianIndividuals
UNION ALL SELECT 'CompanyProfilePaidUpCapitalForeignCompanies', COUNT(*) FROM dbo.CompanyProfilePaidUpCapitalForeignCompanies
UNION ALL SELECT 'CompanyProfilePaidUpCapitalCompaniesMalaysia', COUNT(*) FROM dbo.CompanyProfilePaidUpCapitalCompaniesMalaysia
UNION ALL SELECT 'CompanyProfileCompanyIncorporated', COUNT(*) FROM dbo.CompanyProfileCompanyIncorporated
UNION ALL SELECT 'CompanyProfileCompanyIncorporatedCountries', COUNT(*) FROM dbo.CompanyProfileCompanyIncorporatedCountries
UNION ALL SELECT 'CompanyProfileLoans', COUNT(*) FROM dbo.CompanyProfileLoans
UNION ALL SELECT 'CompanyProfileLoanDomestics', COUNT(*) FROM dbo.CompanyProfileLoanDomestics
UNION ALL SELECT 'CompanyProfileLoanForeigns', COUNT(*) FROM dbo.CompanyProfileLoanForeigns
UNION ALL SELECT 'CompanyProfileTotalFinancings', COUNT(*) FROM dbo.CompanyProfileTotalFinancings
UNION ALL SELECT 'CompanyProfileOtherSources', COUNT(*) FROM dbo.CompanyProfileOtherSources
UNION ALL SELECT 'CompanyProfileUltimateParentHoldingCompanies', COUNT(*) FROM dbo.CompanyProfileUltimateParentHoldingCompanies
ORDER BY TableName;
