using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Documents;

namespace JsonWorkflowEngineRule
{
    public class EligibilityEvaluation : IPlugin
    {
        #region ====== ACTION PARAMS ======

        private const string IN_CaseBenefitLineItemId = "CaseBenifitLineItemId";
        private const string IN_EvaluationContextJson = "EvaluationContextJson";

        private const string OUT_IsEligible = "iseligible";
        private const string OUT_ResultMessage = "resultmessage";
        private const string OUT_ResultJson = "resultJson";

        //Batch Custom API
        private const string IN_CaseBenefitLineItemIdsJson = "CaseBenefitLineItemIdsJson";
        private const string OUT_BatchResultJson = "batchResultJson";



        #endregion

        #region ====== ENTITIES ======

        private const string ENT_BenefitLineItem = "mcg_casebenefitplanlineitem";
        private const string ENT_Case = "incident";
        private const string ENT_ServiceScheme = "mcg_servicescheme";

        private const string ENT_CaseHousehold = "mcg_casehousehold";
        private const string ENT_CaseIncome = "mcg_caseincome";
        private const string ENT_CaseExpense = "mcg_caseexpense";

        private const string ENT_UploadDocument = "mcg_documentextension";

        private const string ENT_CaseAddress = "mcg_caseaddress";

        private const string ENT_EligibilityAdmin = "mcg_eligibilityadmin";
        private const string ENT_EligibilityIncomeRange = "mcg_eligibilityincomerange";
        private const string ENT_SubsidyTableName = "mcg_subsidytablename";
        private const string ENT_ContactTableName = "contact";
        private const string ENT_CaseInvolvedParties = "mcg_caseinvolvedparties";
        private const string ENT_RelationshipRole = "mcg_relationshiprole";

        // ===== WPA Rule #2: Activity Hours =====
        private const string ENT_ContactAssociation = "mcg_relationship";     // Contact Association
        private const string ENT_Income = "mcg_income";                       // Income table (work hours per week is here)
        private const string ENT_EducationDetails = "mcg_educationdetails";   // Education details (education hours per week is here)

        #endregion

        #region ====== FIELDS ======

        // BLI fields
        private const string FLD_BLI_RegardingCase = "mcg_regardingincident";
        private const string FLD_BLI_BenefitUniqId = "mcg_casebenefitplanlineitemid";
        private const string FLD_BLI_EligibilityComments = "mcg_eligibilitycomments";
        private const string FLD_BLI_EligibilityStatus = "mcg_eligibilitystatus";
        private const int ELIG_STATUS_INELIGIBLE = 861450000;
        private const int ELIG_STATUS_ELIGIBLE = 861450001;
        private const int ELIG_STATUS_PENDING = 861450002;



        // Verified? is Choice
        private const string FLD_BLI_Verified = "mcg_verifiedids";

        //Contact fields
        private const string FLD_Con_MaritalStatus = "familystatuscode";
        private const string FLD_Con_ContactID = "mcg_contactid";

        // Choice values (from your screenshot)
        private const int VERIFIED_NO = 568020000;
        private const int VERIFIED_YES = 568020001;

        private const string FLD_BLI_Benefit = "mcg_servicebenefitnames";
        private const string FLD_BLI_RecipientContact = "mcg_recipientcontact";

        // Care validations
        private const string FLD_BLI_CareServiceType = "mcg_careservicetype";
        private const string FLD_BLI_CareServiceLevel = "mcg_careservicelevel";
        private const string FLD_BLI_ServiceFrequency = "mcg_servicebenefitfrequency";
        private const string FLD_BLI_BenefitId = "mcg_name";


        // Service Scheme fields
        private const string FLD_SCHEME_BenefitName = "mcg_benefitname";
        private const string FLD_SCHEME_RuleJson = "mcg_ruledefinitionjson";

        // Case fields
        private const string FLD_CASE_PrimaryContact = "mcg_contact";
        private const string FLD_CASE_IncidentId = "incidentid";
        private const string FLD_CASE_YearlyEligibleIncome = "mcg_yearlyeligibleincome";
        private const string FLD_CASE_YearlyHouseholdIncome = "mcg_yearlyhouseholdincome";
        private const string FLD_BLI_ProgramSpecificDeductions = "mcg_programspecificdeductions";


        // Household fields
        private const string FLD_CH_Case = "mcg_case";
        private const string FLD_CH_Contact = "mcg_contact";
        private const string FLD_CH_DateEntered = "mcg_dateentered";
        private const string FLD_CH_DateExited = "mcg_dateexited";
        private const string FLD_CH_Primary = "mcg_primary";
        private const string FLD_CH_StateCode = "statecode";
        private const string FLD_CH_Relationship = "mcg_relationship";
        private const string FLD_CH_RelationshipRole = "mcg_relationshiprole";

        // Case Income fields
        private const string FLD_CI_Case = "mcg_case";
        private const string FLD_CI_ApplicableIncome = "mcg_applicableincome"; // income applicable
        private const string FLD_CI_IncomeCategory = "mcg_incomecategory";
        private const string FLD_CI_IncomeSubCategory = "mcg_incomesubcategory";
        private const string FLD_CI_Contact = "mcg_contact";
        private const string FLD_CI_ContactIncome = "mcg_casecontactincome";

        // Expense uses same logical name for applicable flag (your update)
        private const string FLD_Common_Case = "mcg_case";

        // Document Extension (category/subcategory are TEXT fields)
        private const string FLD_DOC_Case = "mcg_case";
        private const string FLD_DOC_Contact = "mcg_contact";
        private const string FLD_DOC_Category = "mcg_uploaddocumentcategory";
        private const string FLD_DOC_SubCategory = "mcg_uploaddocumentsubcategory";

        private const string FLD_DOC_Verified = "mcg_verified";
        // Citizenship is on documentextension
        private const string FLD_DOC_ChildCitizenship = "mcg_childcitizenship";
        private const string REQUIRED_CITIZENSHIP = "Montgomery";

        // Case Address fields
        private const string FLD_CA_Case = "mcg_case";
        private const string FLD_CA_EndDate = "mcg_enddate";

        //Eligibility Income Range fields
        private const string FLD_EIR_HouseHoldSize = "mcg_householdsize";
        private const string FLD_EIR_MinIncome = "mcg_minincome";
        private const string FLD_EIR_MaxIncome = "mcg_maxincome";
        private const string FLD_EIR_EligibilityAdmin = "mcg_eligibilityadmin";

        //EligibiliyAdmin
        private const string FLD_EA_Name = "mcg_name";

        //Case Benefit Line Item Already receiving State CCS benefits field
        private const string FLD_CaseBenefit_StateCCSFlag = "mcg_alreadyreceivingstateccsbenefits";

        //Case Expense Table
        private const string FLD_CE_ExpenseType = "mcg_type";
        private const string FLD_CE_Amount = "mcg_amount";

        //Case Involved Parties Fields
        private const string FLD_CIP_CaseRelationShip = "mcg_caserelationship";
        private const string FLD_CIP_CaseId = "mcg_incident";

        //Relationship role entity
        private const string FLD_RR_RRID = "mcg_relationshiproleid";
        private const string FLD_RR_Name = "mcg_name";

        // ===== EICM Notes (mcg_eicmnotes) =====
        private const string ENT_EICMNotes = "mcg_eicmnotes";
        private const string FLD_NOTE_Type = "mcg_notetype";
        private const string FLD_NOTE_Description = "mcg_notedescription";
        private const string FLD_NOTE_CaseLookup = "mcg_incident";
        private const string FLD_NOTE_Name = "mcg_name";
        private const string FLD_NOTE_DescriptionText = "mcg_description";

        // ===== Eligibility Data =====
        private const string ENT_EligibilityData = "mcg_eligibilitydata";

        private const string FLD_ED_ApplicationType = "mcg_applicationtype";
        private const string FLD_ED_ServiceNameBenefitName = "mcg_servicenamebenefitname";
        private const string FLD_ED_NetIncomeBeforeDeduction = "mcg_householdnetincomebeforededuction";
        private const string FLD_ED_DeductionAmount = "mcg_deductionamount";
        private const string FLD_ED_DeductionApplied = "mcg_deductionapplied";
        private const string FLD_ED_NetIncomeAfterDeduction = "mcg_householdnetincomeafterdeduction";
        private const string FLD_ED_ChildName = "mcg_childname";
        private const string FLD_ED_HouseholdSize = "mcg_householdsize";
        private const string FLD_ED_ChildAge = "mcg_childage";
        private const string FLD_ED_ChildDisabledFlag = "mcg_childdisabledflag";
        private const string FLD_ED_Citizenship = "mcg_citizenship";
        private const string FLD_ED_County = "mcg_county";
        private const string FLD_ED_CareLevel = "mcg_carelevel";
        private const string FLD_ED_CareType = "mcg_caretype";
        private const string FLD_ED_BenefitFrequency = "mcg_benefitfrequency";
        private const string FLD_ED_EligibilityAmount = "mcg_eligibilityamount";
        private const string FLD_ED_EligibilityStatus = "mcg_eligibilitystatus";
        private const string FLD_ED_IncomeRange = "mcg_incomerange";
        private const string FLD_ED_IneligibilityReason = "mcg_ineligibilityreason";
        private const string FLD_ED_Verified = "mcg_verified";
        private const string FLD_ED_NumberOfRelativeKids = "mcg_numberofrelativekids";
        private const string FLD_ED_SelfEmployedSingleBothParents = "mcg_selfemployedsinglebothparents";
        private const string FLD_ED_CaseId = "mcg_caseid";
        private const string FLD_ED_BenefitId = "mcg_benefitid";
        private const string FLD_ED_BLI_Lookup = "mcg_casebenefitlineitem";



        // From your screenshot (Eligibility row highlighted)
        private const int NOTE_TYPE_ELIGIBILITY = 861450003;

        //Enum Expense Type
        public enum CaseExpenseType
        {
            MedicalBills = 861450021,
            MedicalPremiumExcludingMedicare = 861450022,
            MedicarePremium = 861450023
        }

        //Enum RelationType Type
        public enum HouseholdRelationship
        {
            SpouseOrPartner = 861450037
        }

        //Enum Involved Parties Case Relationship Choice
        public enum InvolvedPartiesRelationship
        {
            SpouseOrPartner = 861450037,
            OtherParent = 861450007,
            Parent = 861450025,
            OtherFamilyMember = 861450039
        }

        //Enum MaritalStatus
        public enum ContactMaritalStatus
        {
            Single = 1,
            Divorced = 3,
            Separated = 861450003,
            SingleOrNeverMarried = 861450000
        }

        //Enum Income Category from Case income entity
        public enum CaseIncomeCatergory
        {
            EarningsOrWages = 861450000,
            Military = 861450001,
            PublicBenefits = 861450002,
            Other = 861450004
        }


        //Enum Income Sub Category from Case income entity
        public enum CaseIncomeSubCatergory
        {
            ChildSupport = 861450045
        }

        //Enum Case Benefit Line Item Entity State CCS choice column
        public enum CaseStateCCS
        {
            Yes = 568020000,
            NO = 568020001
        }

        //Static Relationship lookup
        public static class CaseRelationShipLookup
        {
            public const string SpouseOrPartner = "Spouse/Partner";
            public const string DomesticPartner = "Domestic Partner";
            public const string OtherParent = "Other Parent";
            public const string Partner = "Partner";
            // FIXED: separate relationship names
            public const string RelativeChild = "Relative Child";
            public const string GrandChild = "Grand Child";

        }

        //Static DocumentCategory Type
        public static class DocumentCategory
        {
            public const string Income = "Income";
            public const string Expenses = "Expenses";
        }

        //Static DocumentSubCategory Type
        public static class DocumentSubCategory
        {
            public const string Paystub = "Paystub";
            public const string W2 = "W-2";
            public const string Expense = "Expense";
            public const string ChildSupport = "Child Support";
            public const string HospitalBill = "Hospital or Medical Bills";
            public const string MedicationCosts = "Medication Costs";
            public const string MedicalReceipts = "Other Medical Receipts";

        }

        // ===== WPA Rule #2: Contact Association fields =====
        private const string FLD_REL_Contact = "mcg_contactid";
        private const string FLD_REL_RelatedContact = "mcg_relatedcontactid";
        private const string FLD_REL_RoleType = "mcg_relationshiproletype"; // lookup
        private const string FLD_REL_EndDate = "mcg_enddate";
        private const string FLD_REL_StateCode = "statecode";

        // ===== WPA Rule #2: Income fields =====
        private const string FLD_INC_Contact = "mcg_contactid";
        private const string FLD_INC_WorkHours = "mcg_workhours"; // Work Hours per week

        // ===== WPA Rule #2: Education fields =====
        private const string FLD_EDU_Contact = "mcg_contactid";
        private const string FLD_EDU_WorkHours = "mcg_workhours"; // Work Hours (education hours per week)

        // ===== WPA Rule #2: Tokens =====
        // Rule JSON should use: token="activityrequirementmet" equals true
        private const string TOKEN_ActivityRequirementMet = "activityrequirementmet";
        private const string TOKEN_EvidenceCareNeededForChild = "evidencecareneededforchild";
        private const string TOKEN_Parent1ActivityHours = "parent1activityhoursperweek";
        private const string TOKEN_Parent2ActivityHours = "parent2activityhoursperweek";
        private const string TOKEN_ParentsTotalActivityHours = "totalactivityhoursperweek";
        private const string TOKEN_ProofIdentityProvided = "proofidentityprovided";
        private const string TOKEN_MostRecentTaxReturnProvided = "mostrecenttaxreturnprovided";
        private const string TOKEN_HasEnrollmentDocument = "hasenrollmentdocument";




        #endregion

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            tracing.Trace("=== EligibilityEvaluationPlugin START ===");

            try
            {
                var bliId = GetGuidFromInput(context, IN_CaseBenefitLineItemId);

                var evalContextJson = context.InputParameters.Contains(IN_EvaluationContextJson)
                    ? context.InputParameters[IN_EvaluationContextJson] as string
                    : null;

                tracing.Trace($"Input BLI Id: {bliId}");
                tracing.Trace($"EvaluationContextJson present: {!string.IsNullOrWhiteSpace(evalContextJson)}");

                var bli = service.Retrieve(ENT_BenefitLineItem, bliId, new ColumnSet(
                    FLD_BLI_RegardingCase,
                    FLD_BLI_Verified,
                    FLD_BLI_Benefit,
                    FLD_BLI_RecipientContact,
                    FLD_BLI_CareServiceType,
                    FLD_BLI_CareServiceLevel,
                    FLD_BLI_ServiceFrequency,
                    FLD_BLI_BenefitId,
                    FLD_BLI_EligibilityStatus

                ));

                var validationFailures = new List<string>();

                var caseRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_RegardingCase);
                if (caseRef == null)
                    validationFailures.Add("Benefit Line Item must be linked to a Case.");

                var benefitRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_Benefit);
                if (benefitRef == null)
                    validationFailures.Add("Financial Benefit (mcg_servicebenefitnames) is missing on Benefit Line Item.");

                // Care validations
                if (!bli.Attributes.Contains(FLD_BLI_CareServiceType) || bli[FLD_BLI_CareServiceType] == null)
                    validationFailures.Add("Care/Service Type (mcg_careservicetype) is missing for the selected child.");

                if (!bli.Attributes.Contains(FLD_BLI_CareServiceLevel) || bli[FLD_BLI_CareServiceLevel] == null)
                    validationFailures.Add("Care/Service Level (mcg_careservicelevel) is missing for the selected child.");

                // Verified? (Choice)
                OptionSetValue verifiedOs = bli.GetAttributeValue<OptionSetValue>(FLD_BLI_Verified);

                bool verifiedIsYes = false;
                if (verifiedOs == null)
                {
                    validationFailures.Add("Verified? (mcg_verifiedids) is not set for the selected child.");
                }
                else if (verifiedOs.Value == VERIFIED_YES)
                {
                    verifiedIsYes = true;
                    tracing.Trace("Verified? = YES => Documented.");
                }
                else if (verifiedOs.Value == VERIFIED_NO)
                {
                    // Per your current implementation (do not change)
                    validationFailures.Add("Verified is No, so the user is Undocumented.");
                }
                else
                {
                    validationFailures.Add($"Verified? has an unexpected value: {verifiedOs.Value}.");
                }

                // Recipient / Beneficiary contact
                var recipientRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_RecipientContact);
                if (recipientRef == null)
                {
                    validationFailures.Add("Beneficiary (Recipient Contact) is missing on Benefit Line Item.");
                }
                else
                {
                    if (caseRef == null)
                    {
                        validationFailures.Add("Case is missing on Benefit Line Item, so activity validation cannot be performed.");
                    }
                    else
                    {
                        validationFailures.AddRange(
                            ValidateWorkHoursNotEmptyForActivity(service, tracing, recipientRef.Id, caseRef.Id)
                        );
                    }

                }

                // -------- Load Case --------
                Entity inc = null;
                EntityReference primaryContactRef = null;

                if (caseRef != null)
                {
                    inc = service.Retrieve(ENT_Case, caseRef.Id, new ColumnSet(FLD_CASE_PrimaryContact, FLD_CASE_YearlyHouseholdIncome, FLD_CASE_YearlyEligibleIncome));
                    primaryContactRef = inc.GetAttributeValue<EntityReference>(FLD_CASE_PrimaryContact);
                    if (primaryContactRef == null)
                        validationFailures.Add("Primary contact is missing on the Case.");
                }

                // -------- Fetch Service Scheme using Benefit Id --------
                string ruleJson = null;
                Entity scheme = null;

                if (benefitRef != null)
                {
                    scheme = GetServiceSchemeForBenefit(service, tracing, benefitRef.Id);
                    if (scheme == null)
                    {
                        validationFailures.Add("No Service Scheme found for the selected Financial Benefit (mcg_servicescheme.mcg_benefitname).");
                    }
                    else
                    {
                        ruleJson = scheme.GetAttributeValue<string>(FLD_SCHEME_RuleJson);
                        if (string.IsNullOrWhiteSpace(ruleJson))
                            validationFailures.Add("No rule were created agaisnt this benefit.");
                    }
                }

                // -------- Validations --------
                if (caseRef != null)
                {
                    // household
                    var household = GetActiveHouseholdIds(service, tracing, caseRef.Id);
                    if (household.Count == 0)
                        validationFailures.Add("No active Case Household members found (Date Exited is blank).");

                    // Validation #2: ANY Case Income row exists (per your change)
                    var hasAnyIncome = HasAnyCaseIncome(service, tracing, caseRef.Id, null, null);
                    if (!hasAnyIncome)
                        validationFailures.Add("Case Income – No case income record found.");

                    // Validation #3: Case Address exists with null/future end date
                    var addressFail = ValidateCaseHomeAddress(service, tracing, caseRef.Id);
                    if (!string.IsNullOrWhiteSpace(addressFail))
                        validationFailures.Add(addressFail);

                    // Validation #1: Citizenship read from Birth Certificate doc (mcg_documentextension)
                    if (recipientRef != null)
                    {
                        var citizenshipFail = ValidateChildCitizenshipFromBirthCertificate(service, tracing, recipientRef.Id);
                        if (!string.IsNullOrWhiteSpace(citizenshipFail))
                            validationFailures.Add(citizenshipFail);
                    }

                    // Proof of Address and Tax Returns (TEXT category/subcategory)
                    if (primaryContactRef != null)
                    {
                        if (!HasDocumentByCategorySubcategory(service, tracing, caseRef.Id, primaryContactRef.Id, "Identification", "Proof of Address"))
                            validationFailures.Add("Proof of address document is missing.");

                        if (!HasDocumentByCategorySubcategory(service, tracing, caseRef.Id, primaryContactRef.Id, "Income", "Tax Returns"))
                            validationFailures.Add("Most recent income tax return document is missing.");
                    }
                    // Has Already receiving the State CCS check
                    var stateCcsMessage = HasAlreadyReceivingStateCCS(service, tracing, bliId);
                    if (!string.IsNullOrWhiteSpace(stateCcsMessage))
                    {
                        validationFailures.Add(stateCcsMessage);
                    }

                }

                // -------- Stop if validations failed --------
                if (validationFailures.Count > 0)
                {
                    tracing.Trace("VALIDATION FAILED. Returning validation failures only.");

                    context.OutputParameters[OUT_IsEligible] = false;
                    context.OutputParameters[OUT_ResultMessage] = "Validation failed. Please fix the issues and try again.";

                    context.OutputParameters[OUT_ResultJson] = BuildResultJson(
                        validationFailures,
                        evaluationLines: null,
                        criteriaSummary: null,
                        parametersConsidered: null,
                        isEligible: false,
                        resultMessage: "Validation failed."
                    );

                    TryUpdateEligibilityComments(service, tracing, bliId, isEligible: false, groupEvals: null, evalLines: null, validationFailures: validationFailures);


                    TryCreateEligibilityNote(
    service,
    tracing,
    bli,
    caseRef,
    benefitRef,
    recipientRef,
    isEligible: false,
    resultMessage: "Validation failed",
    validationFailures: validationFailures,
    evalLines: null,
    groupEvals: null,
    summaryFacts: null
);

                   

                    return;
                }

                // -------- Rule evaluation --------
                var def = ParseRuleDefinition(ruleJson);
                var tokens = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // Add small “facts” (helps summary UI; safe even if you don’t show it)
                var facts = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                facts["benefit.verifiedFlag"] = verifiedIsYes ? "Yes" : "No";

                if (caseRef != null)
                {
                    // Rule 1 token population (income + expense; asset ignored)
                    PopulateRule1Tokens(service, tracing, caseRef.Id, tokens);

                    //Rule 7 token population (Yeary income)
                    PopulateRule7Tokens(context, service, tracing, caseRef.Id, tokens);

                    //Rule 8 token population (Child support, court ordered or voluntary child support?)
                    PopulateRule8Tokens(context, service, tracing, caseRef.Id, tokens);

                    //Rule 9 token population (Medical expense > 2500)
                    PopulateRule9Tokens(context, service, tracing, caseRef.Id, tokens);

                    //Rule 10 token population (Single-parent family)
                    PopulateRule10Tokens(context, service, tracing, caseRef.Id, tokens);

                    // ===== Rule 2 token population (WPA Activity) =====
                    // beneficiary is recipientRef (mcg_recipientcontact)
                    if (recipientRef != null)
                    {
                        PopulateRule2Tokens_WpaActivity(service, tracing, caseRef.Id, recipientRef.Id, tokens, facts);
                        // ===== Rule 3 token population (WPA Evidence Care Needed) is derived inside Rule2 (do not override here)

                        // ===== Rule 4 token population (Proof of Identity for all household members) =====
                        var household = GetActiveHouseholdIds(service, tracing, caseRef.Id);
                        PopulateRule4Tokens_ProofOfIdentity(service, tracing, caseRef.Id, household, tokens, facts);

                        // ===== Rule 5 token population (Proof of Residency) =====
                        PopulateRule5Tokens_ProofOfResidency(service, tracing, caseRef.Id,household, tokens, facts);

                        // ===== Rule 6 token population (Most recent income tax return) =====
                        PopulateRule6Tokens_MostRecentIncomeTaxReturn(service, tracing, caseRef.Id, tokens, facts);

                    }
                }

                var evalLines = new List<EvalLine>();
                var groupEvals = new List<GroupEval>();
                bool overall = EvaluateRuleDefinition(def, tokens, tracing, evalLines, groupEvals);

                // ✅ Update Eligibility Status to "Eligible" when overall passes
                if (overall)
                {
                    TrySetEligibilityStatusEligible(service, tracing, bliId, bli);
                }
                else
                {
                    TrySetEligibilityStatusInEligible(service, tracing, bliId, bli);
                }

                // Criteria summary per top-level rule group (Q1, Q2, ...)
                var criteriaSummary = EvaluateTopLevelGroups(def, tokens, tracing);

                // Parameters considered (Rule 1 only for now) - kept unchanged
                var parametersConsidered = BuildParametersConsideredForRule1(tokens);
                var householdSize = CountHouseHoldSize(service, tracing, caseRef.Id);
                var citizenship = "";

                var summaryFacts = BuildEligibilitySummaryFacts(
                service,
                tracing,
                bli,
                inc,
                caseRef.Id,
                recipientRef.Id,
                householdSize,
                citizenship

);

                context.OutputParameters[OUT_IsEligible] = overall;
                context.OutputParameters[OUT_ResultMessage] = overall ? "Eligible" : "Not Eligible";

                context.OutputParameters[OUT_ResultJson] = BuildResultJson(
                    validationFailures: new List<string>(),
                    evaluationLines: evalLines,
                    criteriaSummary: criteriaSummary,
                    parametersConsidered: parametersConsidered,
                    isEligible: overall,
                    resultMessage: overall ? "Eligible" : "Not Eligible",
                    facts: facts,
                    groupEvals: groupEvals,
                    summaryFacts: summaryFacts
                );
                

                TryUpdateEligibilityComments(service, tracing, bliId, isEligible: overall, groupEvals: groupEvals, evalLines: evalLines, validationFailures: null);


                TryCreateEligibilityNote(
    service,
    tracing,
    bli,
    caseRef,
    benefitRef,
    recipientRef,
    isEligible: overall,
    resultMessage: overall ? "Eligible" : "Not Eligible",
    validationFailures: null,
    evalLines: evalLines,
    groupEvals: groupEvals,
    summaryFacts: summaryFacts
);

                UpsertEligibilityData(
    context,
    service,
    tracing,
    bli,
    caseRef,
    inc,
    benefitRef,
    recipientRef,
    overall,
    groupEvals,
    evalLines
);




                tracing.Trace("Eligibility evaluation completed.");
            }
            catch (Exception ex)
            {
                tracing.Trace("ERROR: " + ex);
                throw new InvalidPluginExecutionException("Eligibility evaluation failed: " + ex.Message, ex);
            }
            finally
            {
                tracing.Trace("=== EligibilityEvaluationPlugin END ===");
            }
        }

        #region ====== Scheme Fetch (Benefit -> Scheme) ======

        private static Entity GetServiceSchemeForBenefit(IOrganizationService svc, ITracingService tracing, Guid benefitId)
        {
            var qe = new QueryExpression(ENT_ServiceScheme)
            {
                ColumnSet = new ColumnSet(FLD_SCHEME_RuleJson, FLD_SCHEME_BenefitName),
                TopCount = 1
            };

            qe.Criteria.AddCondition(FLD_SCHEME_BenefitName, ConditionOperator.Equal, benefitId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // Active
            qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

            var scheme = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();
            tracing.Trace($"GetServiceSchemeForBenefit: benefitId={benefitId}, found={(scheme != null)}");
            return scheme;
        }

        #endregion

        private static string GetChoiceFormattedValue(Entity e, string attributeLogicalName)
        {
            if (e == null || string.IsNullOrWhiteSpace(attributeLogicalName)) return "";

            // Best: formatted label for choice/two-options
            if (e.FormattedValues != null && e.FormattedValues.ContainsKey(attributeLogicalName))
                return (e.FormattedValues[attributeLogicalName] ?? "").Trim();

            if (!e.Attributes.Contains(attributeLogicalName) || e[attributeLogicalName] == null)
                return "";

            var v = e[attributeLogicalName];

            // If someone stored it as string
            if (v is string s) return (s ?? "").Trim();

            // If choice but formatted not present, return numeric as string (fallback)
            if (v is OptionSetValue os) return os.Value.ToString(CultureInfo.InvariantCulture);

            return v.ToString().Trim();
        }


        #region ====== VALIDATIONS ======

        private static int CountSelfEmployedIncomeRecords(IOrganizationService service, ITracingService tracing, Guid caseId)
        {
            const string ENT_CASEINCOME = "mcg_caseincome";
            const string FLD_CASE = "mcg_case";
            const string FLD_SELF = "mcg_selfemployed";
            const string FLD_STATE = "statecode";

            var qe = new QueryExpression(ENT_CASEINCOME)
            {
                ColumnSet = new ColumnSet(FLD_SELF),
                Criteria = new FilterExpression(LogicalOperator.And),
                TopCount = 5000
            };

            qe.Criteria.AddCondition(FLD_CASE, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_STATE, ConditionOperator.Equal, 0);

            var res = service.RetrieveMultiple(qe);
            var count = res.Entities.Count(e => e.GetAttributeValue<bool?>(FLD_SELF) == true);

            tracing.Trace($"Self employed record count = {count}");
            return count;
        }

        private static decimal ApplyProgramSpecificDeductionsAndUpdateCase(
            IOrganizationService svc,
            ITracingService tracing,
            Guid caseId,
            Guid bliId,
            out decimal totalDeductions,
            out int relativeKidsCount,
            out bool medicalDeductionApplied,
            out int selfEmployedCount)
        {
            totalDeductions = 0m;
            relativeKidsCount = 0;
            medicalDeductionApplied = false;
            selfEmployedCount = 0;

            // Base incomes from Case
            var inc = svc.Retrieve(
                ENT_Case,
                caseId,
                new ColumnSet(FLD_CASE_YearlyEligibleIncome, FLD_CASE_YearlyHouseholdIncome)
            );

            var yearlyEligibleIncome = inc.GetAttributeValue<Money>(FLD_CASE_YearlyEligibleIncome)?.Value ?? 0m; // BASE for net
            var yearlyHouseholdIncome = inc.GetAttributeValue<Money>(FLD_CASE_YearlyHouseholdIncome)?.Value ?? 0m; // GROSS for self-emp calc

            // Deduction #1: $5000 per "Relative Child/Grand Child"
            var relativeChildCount = GetActiveHouseholdCount(svc, tracing, caseId, CaseRelationShipLookup.RelativeChild).Count;
            var grandChildCount =
    GetActiveHouseholdCount(
        svc,
        tracing,
        caseId,
        CaseRelationShipLookup.GrandChild
    ).Count;

            relativeKidsCount =  relativeChildCount + grandChildCount;
            var relativeKidsDeduction = relativeKidsCount * 5000m;

            // Deduction #2: if medical bills total > 2500, deduct only 2500
            var medicalTotal = CalculateMedicalExpense(svc, tracing, caseId);
            var medicalDeduction = 0m;
            if (medicalTotal > 2500m)
            {
                medicalDeductionApplied = true;
                medicalDeduction = 2500m;
            }

            // Deduction #3: for EACH self-employed income row, deduct 30% of GROSS household income
            selfEmployedCount = CountSelfEmployedIncomeRecords(svc, tracing, caseId);
            var selfEmpDeduction = (selfEmployedCount > 0 && yearlyHouseholdIncome > 0m)
                ? (yearlyHouseholdIncome * 0.30m) * selfEmployedCount
                : 0m;

            totalDeductions = relativeKidsDeduction + medicalDeduction + selfEmpDeduction;

            // Net after deductions (do not allow negative)
            var netAfter = yearlyEligibleIncome - totalDeductions;
            if (netAfter < 0m) netAfter = 0m;

            tracing.Trace($"[Deductions] yearlyEligibleIncome={yearlyEligibleIncome}, yearlyHouseholdIncome={yearlyHouseholdIncome}");
            tracing.Trace($"[Deductions] relativeKidsCount={relativeKidsCount}, relativeKidsDeduction={relativeKidsDeduction}");
            tracing.Trace($"[Deductions] medicalTotal={medicalTotal}, medicalDeductionApplied={medicalDeductionApplied}, medicalDeduction={medicalDeduction}");
            tracing.Trace($"[Deductions] selfEmployedCount={selfEmployedCount}, selfEmpDeduction={selfEmpDeduction}");
            tracing.Trace($"[Deductions] totalDeductions={totalDeductions}, netAfter={netAfter}");

            // Update Case.mcg_programspecificdeductions
            try
            {
                var upd = new Entity(ENT_BenefitLineItem) { Id = bliId };
                upd[FLD_BLI_ProgramSpecificDeductions] = new Money(totalDeductions);
                svc.Update(upd);
            }
            catch (Exception ex)
            {
                tracing.Trace("WARNING: Failed to update mcg_programspecificdeductions: " + ex.Message);
                // Non-blocking; do not fail eligibility for this.
            }

            return netAfter;
        }


        private static bool HasAnyCaseIncome(IOrganizationService svc, ITracingService tracing, Guid caseId, CaseIncomeCatergory[] caseIncomeCategories, CaseIncomeSubCatergory[] caseIncomeSubCategories)
        {
            var qe = new QueryExpression(ENT_CaseIncome)
            {
                ColumnSet = new ColumnSet("mcg_caseincomeid"),
                TopCount = 1
            };

            qe.Criteria.AddCondition(FLD_CI_Case, ConditionOperator.Equal, caseId);

            bool hasCategories = caseIncomeCategories?.Any() == true;
            bool hasSubCategories = caseIncomeSubCategories?.Any() == true;

            if (hasCategories && hasSubCategories)
            {
                var dependentFilter = new FilterExpression(LogicalOperator.And);

                dependentFilter.AddCondition(
                    FLD_CI_IncomeCategory,
                    ConditionOperator.In,
                    caseIncomeCategories.Select(c => (object)(int)c).ToArray());

                dependentFilter.AddCondition(
                    FLD_CI_IncomeSubCategory,
                    ConditionOperator.In,
                    caseIncomeSubCategories.Select(sc => (object)(int)sc).ToArray());

                qe.Criteria.AddFilter(dependentFilter);

                tracing.Trace("Applied IncomeCategory and IncomeSubCategory filters");
            }
            else
            {
                tracing.Trace("No IncomeCategory and SubCategory filters applied");
            }


            var found = svc.RetrieveMultiple(qe).Entities.Any();
            tracing.Trace($"HasAnyCaseIncome(caseId={caseId}) = {found}");
            return found;
        }

        private static string ValidateCaseHomeAddress(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            try
            {
                var qe = new QueryExpression(ENT_CaseAddress)
                {
                    ColumnSet = new ColumnSet(FLD_CA_EndDate)
                };
                qe.Criteria.AddCondition(FLD_CA_Case, ConditionOperator.Equal, caseId);

                var rows = svc.RetrieveMultiple(qe).Entities.ToList();
                tracing.Trace($"CaseAddress rows found: {rows.Count}");

                if (rows.Count == 0)
                    return "Home address is missing on Case (no mcg_caseaddress records found).";

                var today = DateTime.UtcNow.Date;

                bool hasActive = rows.Any(r =>
                {
                    var end = r.GetAttributeValue<DateTime?>(FLD_CA_EndDate);
                    return !end.HasValue || end.Value.Date >= today;
                });

                if (!hasActive)
                    return "Home address is missing on Case (no address with a Null or Future End Date).";

                tracing.Trace("PASS: Case home address validation.");
                return null;
            }
            catch (Exception ex)
            {
                tracing.Trace("ValidateCaseHomeAddress ERROR: " + ex);
                return "Unable to validate Case Home Address due to an internal error.";
            }
        }

        private static string ValidateChildCitizenshipFromBirthCertificate(IOrganizationService svc, ITracingService tracing, Guid beneficiaryContactId)
        {
            try
            {
                var qe = new QueryExpression(ENT_UploadDocument)
                {
                    ColumnSet = new ColumnSet("createdon", FLD_DOC_ChildCitizenship),
                    TopCount = 1
                };

                qe.Criteria.AddCondition(FLD_DOC_Contact, ConditionOperator.Equal, beneficiaryContactId);
                qe.Criteria.AddCondition(FLD_DOC_Category, ConditionOperator.Equal, "Verifications");
                qe.Criteria.AddCondition(FLD_DOC_SubCategory, ConditionOperator.Equal, "Birth Certificate");
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

                qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

                var doc = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();
                var hasBirthCert = (doc != null);

                tracing.Trace($"Birth Certificate doc found for beneficiary={beneficiaryContactId}: {hasBirthCert}");

                if (!hasBirthCert)
                    return "No document is present for beneficiary under Verifications > Birth Certificate.";

                var citizenship = (doc.GetAttributeValue<string>(FLD_DOC_ChildCitizenship) ?? "").Trim();

                if (string.IsNullOrWhiteSpace(citizenship))
                    return "Child citizenship is missing on Birth Certificate document (mcg_childcitizenship).";

                //if (!string.Equals(citizenship, REQUIRED_CITIZENSHIP, StringComparison.OrdinalIgnoreCase))
                //    return $"Child citizenship does not match {REQUIRED_CITIZENSHIP} (Current: {citizenship}).";

                tracing.Trace($"PASS: Child citizenship validated from document. Citizenship='{citizenship}'.");
                return null;
            }
            catch (Exception ex)
            {
                tracing.Trace("ValidateChildCitizenshipFromBirthCertificate ERROR: " + ex);
                return "Unable to validate Child Citizenship due to an internal error.";
            }
        }

        private static string HasAlreadyReceivingStateCCS(IOrganizationService svc, ITracingService tracing, Guid bliId)
        {
            var qe = new QueryExpression(ENT_BenefitLineItem)
            {
                ColumnSet = new ColumnSet(FLD_CaseBenefit_StateCCSFlag, "statecode"),
                TopCount = 1
            };

            qe.Criteria.AddCondition(FLD_BLI_BenefitUniqId, ConditionOperator.Equal, bliId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var entity = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();

            if (entity == null)
            {
                tracing.Trace("Case benefit line item not found");
                return string.Empty;
            }

            if (!entity.Attributes.Contains(FLD_CaseBenefit_StateCCSFlag))
            {
                tracing.Trace("CaseStateCCS is NULL");
                return "Please fill in if the beneficiary is already receiving the STATE CCS benefit";
            }

            var stateCcs = entity.GetAttributeValue<OptionSetValue>(FLD_CaseBenefit_StateCCSFlag)?.Value;

            if (stateCcs == (int)CaseStateCCS.Yes)
            {
                tracing.Trace("CaseStateCCS = Yes");
                return "You are already receiving the benefit from the STATE CCS";
            }

            tracing.Trace("CaseStateCCS is present but not Yes");
            return string.Empty;
        }

        private static bool CheckMaritalStatusAllowedFromCaseContact(IOrganizationService svc, ITracingService tracing, Guid caseId, params ContactMaritalStatus[] maritalStatus)
        {
            tracing.Trace("CheckMaritalStatusAllowedFromCase method start");

            Entity caseRecord = svc.Retrieve(
                ENT_Case,
                caseId,
                new ColumnSet(FLD_CASE_PrimaryContact));

            EntityReference primaryContactRef = caseRecord.GetAttributeValue<EntityReference>(FLD_CASE_PrimaryContact);

            bool isMaritalStatus = false;

            if (primaryContactRef == null)
            {
                tracing.Trace("Primary contact is missing on the Case.");
            }
            else
            {
                Entity contact = svc.Retrieve(
                    ENT_ContactTableName,
                    primaryContactRef.Id,
                    new ColumnSet(FLD_Con_MaritalStatus, FLD_Con_ContactID));



                OptionSetValue maritalStatusValue =
                    contact.GetAttributeValue<OptionSetValue>(FLD_Con_MaritalStatus);

                if (maritalStatusValue != null &&
                    maritalStatus
                        .Select(ms => (int)ms)
                        .Contains(maritalStatusValue.Value))
                {
                    isMaritalStatus = true;
                }

                tracing.Trace($"Marital status allowed: {isMaritalStatus}");
            }

            tracing.Trace("CheckMaritalStatusAllowedFromCase method end");
            return isMaritalStatus;
        }

        private List<string> ValidateWorkHoursNotEmptyForActivity (IOrganizationService svc,ITracingService tracing, Guid beneficiaryContactId, Guid caseId)
        {
            var failures = new List<string>();
            
            var parentIds = GetActiveParentsForBeneficiary(svc,tracing, beneficiaryContactId);   

            if (parentIds == null || parentIds.Count == 0)
            {
                tracing.Trace("No active parents found for beneficiary.");
                return failures;
            }
            foreach (var parentId in parentIds)
            {
                var parentName = TryGetContactFullName(svc, tracing, parentId) ?? parentId.ToString();
                // 1) Get applicable Case Income rows for this Case + Parent
                var ciQ = new QueryExpression(ENT_CaseIncome)
                {
                    ColumnSet = new ColumnSet(FLD_CI_ContactIncome),
                    TopCount = 500
                };
                ciQ.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                ciQ.Criteria.AddCondition(FLD_CI_Case, ConditionOperator.Equal, caseId);
                ciQ.Criteria.AddCondition(FLD_CI_Contact, ConditionOperator.Equal, parentId);
                ciQ.Criteria.AddCondition(FLD_CI_ApplicableIncome, ConditionOperator.Equal, true);

                var caseIncomeRows = svc.RetrieveMultiple(ciQ).Entities;

                // 2) Extract income ids from mcg_contactincome lookup
                var incomeIds = caseIncomeRows
                    .Select(x => x.GetAttributeValue<EntityReference>(FLD_CI_ContactIncome))
                    .Where(r => r != null)
                    .Select(r => r.Id)
                    .Distinct()
                    .ToList();

                // 3) Only validate workhours if we actually have mapped applicable income rows
                if (incomeIds.Count > 0)
                {
                    var incQ = new QueryExpression(ENT_Income)
                    {
                        ColumnSet = new ColumnSet(FLD_INC_WorkHours),
                        TopCount = 500
                    };
                    incQ.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                    incQ.Criteria.AddCondition("mcg_incomeid", ConditionOperator.In, incomeIds.Cast<object>().ToArray());

                    var incomeRows = svc.RetrieveMultiple(incQ).Entities;

                    bool hasMissingWorkHours = incomeRows.Any(r =>
                        !r.Attributes.Contains(FLD_INC_WorkHours) || r[FLD_INC_WorkHours] == null);

                    if (hasMissingWorkHours)
                    {
                        failures.Add($"Please add the employment hours per week in the Income Section.Missing Parent Name - {parentName}.");
                    }
                }

                var eduQ = new QueryExpression(ENT_EducationDetails)
                {
                    ColumnSet = new ColumnSet("mcg_educationdetailsid"),
                    TopCount = 1
                };

                eduQ.Criteria.AddCondition("statecode",ConditionOperator.Equal,0);
                eduQ.Criteria.AddCondition("mcg_contactid", ConditionOperator.Equal, parentId);
                eduQ.Criteria.AddCondition("mcg_workhours", ConditionOperator.Null);

                var eduMissing = svc.RetrieveMultiple(eduQ).Entities.Any();
                if(eduMissing)
                {
                    failures.Add($"Please add the education/certificate hours in Educational details section.Missing Parent Name - {parentName}.");
                }
            }

            return failures;
        }

        private static List<Entity> CheckCaseInvolvedParties(IOrganizationService svc, ITracingService tracing, Guid caseId, InvolvedPartiesRelationship[] caseRelationship)
        {
            tracing.Trace($"CheckCaseInvolvedParties Method is called");
            var qe = new QueryExpression(ENT_CaseInvolvedParties)
            {
                ColumnSet = new ColumnSet(
                    FLD_CIP_CaseRelationShip, FLD_CIP_CaseId
                )
            };

            qe.Criteria.AddCondition(FLD_CIP_CaseId, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_CH_StateCode, ConditionOperator.Equal, 0);
            if (caseRelationship != null && caseRelationship.Length > 0)
            {
                qe.Criteria.AddCondition(
                   FLD_CIP_CaseRelationShip,
                   ConditionOperator.In,
                   caseRelationship.Select(r => (object)(int)r).ToArray()
               );
            }

            var results = svc.RetrieveMultiple(qe).Entities.ToList();
            tracing.Trace($"CheckCaseInvolvedParties count: {results.Count}");
            tracing.Trace($"CheckCaseInvolvedParties Method is end");
            return results;

        }

        private static bool HasDocumentByCategorySubcategory(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId,
    Guid? contactId,
    string category,
    string subCategory)
        {
            var qe = new QueryExpression(ENT_UploadDocument)
            {
                ColumnSet = new ColumnSet("createdon"),
                TopCount = 1
            };

            qe.Criteria.AddCondition(FLD_DOC_Case, ConditionOperator.Equal, caseId);

            // ? FIX: Only filter by contact when contactId is provided
            if (contactId.HasValue && contactId.Value != Guid.Empty)
            {
                qe.Criteria.AddCondition(FLD_DOC_Contact, ConditionOperator.Equal, contactId.Value);
            }
            else
            {
                // contactId null means: "any contact" (don’t apply a contact filter)
                tracing.Trace("HasDocumentByCategorySubcategory: contactId is null/empty => not filtering by contact (ANY contact).");
            }

            qe.Criteria.AddCondition(FLD_DOC_Category, ConditionOperator.Equal, category);
            qe.Criteria.AddCondition(FLD_DOC_SubCategory, ConditionOperator.Equal, subCategory);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var found = svc.RetrieveMultiple(qe).Entities.Any();
            var contactText = (contactId.HasValue && contactId.Value != Guid.Empty) ? contactId.Value.ToString() : "ANY";
            tracing.Trace($"HasDocumentByCategorySubcategory(case={caseId}, contact={contactText}, {category}/{subCategory}) = {found}");
            return found;
        }

        private static string GetCitizenshipFromBirthCertificate(
    IOrganizationService svc,
    ITracingService tracing,
    Guid beneficiaryContactId)
        {
            try
            {
                var qe = new QueryExpression(ENT_UploadDocument)
                {
                    ColumnSet = new ColumnSet("createdon", FLD_DOC_ChildCitizenship),
                    TopCount = 1
                };

                qe.Criteria.AddCondition(FLD_DOC_Contact, ConditionOperator.Equal, beneficiaryContactId);
                qe.Criteria.AddCondition(FLD_DOC_Category, ConditionOperator.Equal, "Verifications");
                qe.Criteria.AddCondition(FLD_DOC_SubCategory, ConditionOperator.Equal, "Birth Certificate");
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

                qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

                var doc = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();
                if (doc == null)
                {
                    tracing.Trace("[Citizenship] No Birth Certificate doc found.");
                    return "";
                }

                var citizenship = (doc.GetAttributeValue<string>(FLD_DOC_ChildCitizenship) ?? "").Trim();
                tracing.Trace($"[Citizenship] mcg_childcitizenship='{citizenship}'");
                return citizenship;
            }
            catch (Exception ex)
            {
                tracing.Trace("[Citizenship] GetCitizenshipFromBirthCertificate failed: " + ex);
                return "";
            }
        }


        private static string GetCountyFromCaseAddress(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId)
        {
            try
            {
                // mcg_caseaddress fields
                const string FLD_Case = "mcg_case";
                const string FLD_EndDate = "mcg_enddate";
                const string FLD_AddressLookup = "mcg_address";

                // mcg_address fields
                const string ENT_Address = "mcg_address";
                const string FLD_CountyText = "mcg_countytext";

                var qe = new QueryExpression(ENT_CaseAddress)
                {
                    ColumnSet = new ColumnSet(FLD_EndDate, FLD_AddressLookup),
                    TopCount = 50
                };

                qe.Criteria.AddCondition(FLD_Case, ConditionOperator.Equal, caseId);

                var rows = svc.RetrieveMultiple(qe).Entities;
                if (rows == null || rows.Count == 0)
                {
                    tracing.Trace("[County] No mcg_caseaddress found.");
                    return "";
                }

                var today = DateTime.UtcNow.Date;

                // pick active address: end null OR end >= today
                var active = rows
                    .OrderByDescending(r => r.GetAttributeValue<DateTime?>("createdon") ?? DateTime.MinValue)
                    .FirstOrDefault(r =>
                    {
                        var end = r.GetAttributeValue<DateTime?>(FLD_EndDate);
                        return !end.HasValue || end.Value.Date >= today;
                    });

                if (active == null)
                {
                    tracing.Trace("[County] No ACTIVE mcg_caseaddress found (enddate null/future).");
                    return "";
                }

                var addrRef = active.GetAttributeValue<EntityReference>(FLD_AddressLookup);
                if (addrRef == null || addrRef.Id == Guid.Empty)
                {
                    tracing.Trace("[County] mcg_caseaddress.mcg_address is null.");
                    return "";
                }

                var addr = svc.Retrieve(ENT_Address, addrRef.Id, new ColumnSet(FLD_CountyText));
                var county = (addr.GetAttributeValue<string>(FLD_CountyText) ?? "").Trim();

                tracing.Trace($"[County] County='{county}' from mcg_address.mcg_countytext");
                return county;
            }
            catch (Exception ex)
            {
                tracing.Trace("[County] GetCountyFromCaseAddress failed: " + ex);
                return "";
            }
        }

        private static void TrySetEligibilityStatusInEligible(
            IOrganizationService service,
            ITracingService tracing,
            Guid bliId,
            Entity bliLoaded)
        {
            try
            {
                // Use already-loaded value if present
                var current = bliLoaded?.GetAttributeValue<OptionSetValue>(FLD_BLI_EligibilityStatus)?.Value;

                if (current.HasValue && current.Value == ELIG_STATUS_INELIGIBLE)
                {
                    tracing.Trace($"EligibilityStatus already Eligible for BLI {bliId}. No update needed.");
                    return;
                }

                var upd = new Entity(ENT_BenefitLineItem, bliId);
                upd[FLD_BLI_EligibilityStatus] = new OptionSetValue(ELIG_STATUS_INELIGIBLE);

                service.Update(upd);

                tracing.Trace($"Updated BLI {bliId} mcg_eligibilitystatus => InEligible ({ELIG_STATUS_INELIGIBLE}).");
            }
            catch (Exception ex)
            {
                // Don't fail eligibility evaluation just because status update failed
                tracing.Trace("Failed to update mcg_eligibilitystatus to Eligible. " + ex);
            }
        }

        private static bool HasVerifiedDocumentByCategoryAndAnySubcategory(
            IOrganizationService svc,
            ITracingService tracing,
            Guid caseId,
            Guid contactId,
            IEnumerable<string> categoryOptions,
            IEnumerable<string> subCategoryOptions)
        {
            var qe = new QueryExpression(ENT_UploadDocument)
            {
                ColumnSet = new ColumnSet(FLD_DOC_Category, FLD_DOC_SubCategory, FLD_DOC_Verified),
                TopCount = 50
            };

            qe.Criteria.AddCondition(FLD_DOC_Case, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_DOC_Contact, ConditionOperator.Equal, contactId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            qe.Criteria.AddCondition(FLD_DOC_Verified, ConditionOperator.Equal, true);

            // category options (OR)
            var catFilter = new FilterExpression(LogicalOperator.Or);
            foreach (var c in (categoryOptions ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
                catFilter.AddCondition(FLD_DOC_Category, ConditionOperator.Equal, c);
            if (catFilter.Conditions.Count > 0)
                qe.Criteria.AddFilter(catFilter);

            // subcategory options (OR)
            var subFilter = new FilterExpression(LogicalOperator.Or);
            foreach (var s in (subCategoryOptions ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
                subFilter.AddCondition(FLD_DOC_SubCategory, ConditionOperator.Equal, s);
            if (subFilter.Conditions.Count > 0)
                qe.Criteria.AddFilter(subFilter);

            var rows = svc.RetrieveMultiple(qe).Entities;
            var found = rows.Any();
            tracing.Trace($"HasVerifiedDocumentByCategoryAndAnySubcategory(case={caseId}, contact={contactId}) => {found} (rows={rows.Count})");
            return found;
        }

        private static void PopulateRule4Tokens_ProofOfIdentity(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId,
    List<Guid> householdContactIds,
    Dictionary<string, object> tokens,
    Dictionary<string, object> facts)
        {
            try
            {
                // Token name used in your rule JSON
                const string TOKEN_ProofIdentityProvidedLocal = "proofidentityprovided";

                if (householdContactIds == null || householdContactIds.Count == 0)
                {
                    tokens[TOKEN_ProofIdentityProvidedLocal] = false;

                    facts["docs.identity.householdCount"] = 0;
                    facts["docs.identity.verifiedCount"] = 0;
                    facts["docs.identity.missingCount"] = 0;
                    facts["docs.identity.missingNames"] = "";
                    return;
                }

                // Allowed identity doc types (based on your functional screenshot text)
                var allowedCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Verification",
            "Verifications"
        };

                var allowedSubCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Birth Certificate",
            "Driver’s License",
            "Driver's License",
            "Passport",
            "Identification Card"
        };

                // Pull docs for this case + household contacts (Active only)
                var qe = new QueryExpression(ENT_UploadDocument)
                {
                    ColumnSet = new ColumnSet(FLD_DOC_Contact, FLD_DOC_Category, FLD_DOC_SubCategory, FLD_DOC_Verified),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };

                qe.Criteria.AddCondition(FLD_DOC_Case, ConditionOperator.Equal, caseId);
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

                // Contact IN household ids
                qe.Criteria.AddCondition(new ConditionExpression(
                    FLD_DOC_Contact,
                    ConditionOperator.In,
                    householdContactIds.Cast<object>().ToArray()
                ));

                var docs = svc.RetrieveMultiple(qe).Entities;

                // Initialize per-contact "has verified identity doc"
                var hasDoc = householdContactIds.ToDictionary(id => id, id => false);

                foreach (var d in docs)
                {
                    var contactRef = d.GetAttributeValue<EntityReference>(FLD_DOC_Contact);
                    if (contactRef == null || contactRef.Id == Guid.Empty) continue;
                    if (!hasDoc.ContainsKey(contactRef.Id)) continue;

                    var cat = (d.GetAttributeValue<string>(FLD_DOC_Category) ?? "").Trim();
                    var sub = (d.GetAttributeValue<string>(FLD_DOC_SubCategory) ?? "").Trim();

                    if (!allowedCategory.Contains(cat)) continue;
                    if (!allowedSubCategories.Contains(sub)) continue;

                    var verified = IsYes(d, FLD_DOC_Verified);
                    if (!verified) continue;

                    hasDoc[contactRef.Id] = true;
                }

                var missingIds = hasDoc.Where(x => !x.Value).Select(x => x.Key).ToList();
                var missingNames = new List<string>();

                foreach (var id in missingIds)
                {
                    var name = TryGetContactFullName(svc, tracing, id);
                    missingNames.Add(string.IsNullOrWhiteSpace(name) ? id.ToString() : name);
                }

                var allOk = missingIds.Count == 0;

                tokens[TOKEN_ProofIdentityProvidedLocal] = allOk;

                facts["docs.identity.householdCount"] = householdContactIds.Count;
                facts["docs.identity.verifiedCount"] = householdContactIds.Count - missingIds.Count;
                facts["docs.identity.missingCount"] = missingIds.Count;
                facts["docs.identity.missingNames"] = string.Join(", ", missingNames);

                tracing.Trace($"[Rule4] proofidentityprovided={allOk}; household={householdContactIds.Count}; missing={missingIds.Count}; missingNames={facts["docs.identity.missingNames"]}");
            }
            catch (Exception ex)
            {
                tracing.Trace("[Rule4] PopulateRule4Tokens_ProofOfIdentity failed: " + ex);
                tokens["proofidentityprovided"] = false;
                facts["docs.identity.missingNames"] = "";
            }
        }

        private static void PopulateRule5Tokens_ProofOfResidency(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId,
    List<Guid> householdContactIds,
    Dictionary<string, object> tokens,
    Dictionary<string, object> facts)
        {
            try
            {
                const string TOKEN_ProofResidencyProvidedLocal = "proofresidencyprovided";

                // defaults
                facts["docs.residency.householdCount"] = householdContactIds?.Count ?? 0;
                facts["docs.residency.verifiedCount"] = 0;
                facts["docs.residency.missingCount"] = 0;
                facts["docs.residency.missingNames"] = "";
                facts["docs.residency.hasActiveAddress"] = false;

                // Safety: no household
                if (householdContactIds == null || householdContactIds.Count == 0)
                {
                    tokens[TOKEN_ProofResidencyProvidedLocal] = false;
                    return;
                }

                // 1) Case Address check (active = enddate null or future)
                var qeAddr = new QueryExpression(ENT_CaseAddress)
                {
                    ColumnSet = new ColumnSet(FLD_CA_EndDate),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                qeAddr.Criteria.AddCondition(FLD_CA_Case, ConditionOperator.Equal, caseId);

                var addresses = svc.RetrieveMultiple(qeAddr).Entities.ToList();
                var today = DateTime.UtcNow.Date;

                bool hasActiveAddress = addresses.Any(a =>
                {
                    var end = a.GetAttributeValue<DateTime?>(FLD_CA_EndDate);
                    return !end.HasValue || end.Value.Date >= today;
                });

                facts["docs.residency.hasActiveAddress"] = hasActiveAddress;

                if (!hasActiveAddress)
                {
                    // Residency fails (no active address)
                    tokens[TOKEN_ProofResidencyProvidedLocal] = false;
                    tracing.Trace("[Rule5] No active case address => proofresidencyprovided=false");
                    return;
                }

                // 2) Supporting residency doc (Verified) PER household member
                // Keep the same doc logic you already had, but applied per contact
                bool MatchesResidencyDoc(string cat, string sub)
                {
                    cat = (cat ?? "").Trim();
                    sub = (sub ?? "").Trim();

                    return
                        (cat.Equals("Identification", StringComparison.OrdinalIgnoreCase) &&
                         sub.Equals("Proof of Address", StringComparison.OrdinalIgnoreCase))
                        ||
                        ((cat.Equals("Verification", StringComparison.OrdinalIgnoreCase) || cat.Equals("Verifications", StringComparison.OrdinalIgnoreCase)) &&
                         (sub.Equals("Driver’s License", StringComparison.OrdinalIgnoreCase) ||
                          sub.Equals("Driver's License", StringComparison.OrdinalIgnoreCase) ||
                          sub.Equals("Proof of Address", StringComparison.OrdinalIgnoreCase)));
                }

                // Pull docs for this case + household contacts (Active only, Verified only)
                var qeDoc = new QueryExpression(ENT_UploadDocument)
                {
                    ColumnSet = new ColumnSet(FLD_DOC_Contact, FLD_DOC_Category, FLD_DOC_SubCategory, FLD_DOC_Verified),
                    Criteria = new FilterExpression(LogicalOperator.And),
                    TopCount = 500
                };

                qeDoc.Criteria.AddCondition(FLD_DOC_Case, ConditionOperator.Equal, caseId);
                qeDoc.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                qeDoc.Criteria.AddCondition(FLD_DOC_Verified, ConditionOperator.Equal, true);

                qeDoc.Criteria.AddCondition(new ConditionExpression(
                    FLD_DOC_Contact,
                    ConditionOperator.In,
                    householdContactIds.Cast<object>().ToArray()
                ));

                var docs = svc.RetrieveMultiple(qeDoc).Entities;

                // per contact flag
                var hasDoc = householdContactIds.ToDictionary(id => id, id => false);

                foreach (var d in docs)
                {
                    var cRef = d.GetAttributeValue<EntityReference>(FLD_DOC_Contact);
                    if (cRef == null || cRef.Id == Guid.Empty) continue;
                    if (!hasDoc.ContainsKey(cRef.Id)) continue;

                    var cat = d.GetAttributeValue<string>(FLD_DOC_Category);
                    var sub = d.GetAttributeValue<string>(FLD_DOC_SubCategory);

                    if (!MatchesResidencyDoc(cat, sub)) continue;

                    hasDoc[cRef.Id] = true;
                }

                var missingIds = hasDoc.Where(x => !x.Value).Select(x => x.Key).ToList();
                var missingNames = new List<string>();

                foreach (var id in missingIds)
                {
                    var name = TryGetContactFullName(svc, tracing, id);
                    missingNames.Add(string.IsNullOrWhiteSpace(name) ? id.ToString() : name);
                }

                bool allOk = missingIds.Count == 0;

                tokens[TOKEN_ProofResidencyProvidedLocal] = allOk;

                facts["docs.residency.householdCount"] = householdContactIds.Count;
                facts["docs.residency.verifiedCount"] = householdContactIds.Count - missingIds.Count;
                facts["docs.residency.missingCount"] = missingIds.Count;
                facts["docs.residency.missingNames"] = string.Join(", ", missingNames);

                tracing.Trace($"[Rule5] proofresidencyprovided={allOk}; household={householdContactIds.Count}; missing={missingIds.Count}; missingNames={facts["docs.residency.missingNames"]}");
            }
            catch (Exception ex)
            {
                tracing.Trace("[Rule5] PopulateRule5Tokens_ProofOfResidency failed: " + ex);
                tokens["proofresidencyprovided"] = false;
                facts["docs.residency.missingNames"] = "";
                facts["docs.residency.missingCount"] = 0;
                facts["docs.residency.hasActiveAddress"] = false;
            }
        }


        private static void PopulateRule6Tokens_MostRecentIncomeTaxReturn(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId,
    Dictionary<string, object> tokens,
    Dictionary<string, object> facts)
        {
            try
            {
                // Token used in your rule JSON
                const string TOKEN_Local = "mostrecenttaxreturnprovided";

                // We only need "attached" docs (not verified requirement as per Rule 6 text)
                // Category/SubCategory are TEXT fields
                var allowedCategory = "Income";

                var allowedSubCats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "W-2",
            "Tax Returns",
            "Paystub",
            "Bank Statement",
            "Pay Stubs",        // safety: if someone saved label differently
            "Paystub(s)"        // safety
        };

                var qe = new QueryExpression(ENT_UploadDocument)
                {
                    ColumnSet = new ColumnSet(FLD_DOC_Category, FLD_DOC_SubCategory, FLD_DOC_Contact, "createdon"),
                    Criteria = new FilterExpression(LogicalOperator.And),
                    TopCount = 200
                };

                qe.Criteria.AddCondition(FLD_DOC_Case, ConditionOperator.Equal, caseId);
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                qe.Criteria.AddCondition(FLD_DOC_Category, ConditionOperator.Equal, allowedCategory);

                // subcategory OR filter
                var subOr = new FilterExpression(LogicalOperator.Or);
                foreach (var s in allowedSubCats)
                    subOr.AddCondition(FLD_DOC_SubCategory, ConditionOperator.Equal, s);
                qe.Criteria.AddFilter(subOr);

                // latest first (nice for facts)
                qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

                var docs = svc.RetrieveMultiple(qe).Entities;
                var match = docs.FirstOrDefault();

                bool pass = (match != null);

                tokens[TOKEN_Local] = pass;

                // facts for UI/debug
                facts["rule6.docs.countMatched"] = docs.Count;
                if (pass)
                {
                    var cat = (match.GetAttributeValue<string>(FLD_DOC_Category) ?? "").Trim();
                    var sub = (match.GetAttributeValue<string>(FLD_DOC_SubCategory) ?? "").Trim();

                    var cRef = match.GetAttributeValue<EntityReference>(FLD_DOC_Contact);
                    var who = (cRef != null && cRef.Id != Guid.Empty)
                        ? (TryGetContactFullName(svc, tracing, cRef.Id) ?? cRef.Id.ToString())
                        : "N/A";

                    facts["rule6.docs.matchedDocInfo"] = $"{cat} / {sub} - Contact: {who}";
                }
                else
                {
                    facts["rule6.docs.matchedDocInfo"] = "";
                }

                tracing.Trace($"[Rule6] mostrecenttaxreturnprovided={pass}; matchedRows={docs.Count}; info='{facts["rule6.docs.matchedDocInfo"]}'");
            }
            catch (Exception ex)
            {
                tracing.Trace("[Rule6] PopulateRule6Tokens_MostRecentIncomeTaxReturn failed: " + ex);
                tokens["mostrecenttaxreturnprovided"] = false;
                facts["rule6.docs.countMatched"] = 0;
                facts["rule6.docs.matchedDocInfo"] = "";
            }
        }





        #endregion

        // ----------------------------
        // Eligibility Comments (BLI)
        // ----------------------------
        private static void TryUpdateEligibilityComments(
            IOrganizationService service,
            ITracingService tracing,
            Guid benefitLineItemId,
            bool isEligible,
            List<GroupEval> groupEvals,
            List<EvalLine> evalLines,
            List<string> validationFailures
        )
        {
            try
            {
                string comments = string.Empty;

                // If validations failed (no rule evaluation), store a compact message (optional).
                if (validationFailures != null && validationFailures.Count > 0)
                {
                    comments = "Validation failed: " + string.Join("; ",
                        validationFailures.Where(v => !string.IsNullOrWhiteSpace(v)).Take(5));
                }
                else if (!isEligible)
                {
                    comments = GetAllDenialReasonsText(groupEvals, evalLines);
                }
                else
                {
                    // Eligible: clear comments to avoid stale denial reasons.
                    comments = string.Empty;
                }

                var upd = new Entity(ENT_BenefitLineItem, benefitLineItemId);
                upd[FLD_BLI_EligibilityComments] = comments ?? string.Empty;
                service.Update(upd);

                tracing.Trace("EligibilityComments updated for BLI {0}. isEligible={1}. comments='{2}'",
                    benefitLineItemId, isEligible, comments);
            }
            catch (Exception ex)
            {
                tracing.Trace("TryUpdateEligibilityComments FAILED (ignored): " + ex);
            }
        }

        private void MarkHasEligibilityData(IOrganizationService svc, Guid bliId, ITracingService tracing)
        {
            try
            {
                var bli = new Entity("mcg_casebenefitplanlineitem", bliId);
                bli["mcg_haseligibilitydata"] = true;
                svc.Update(bli);
            }
            catch (Exception ex)
            {
                tracing.Trace("MarkHasEligibilityData FAILED (ignored): " + ex);
                // ignore - do not block eligibility evaluation
            }
        }


        private static string GetAllDenialReasonsText(List<GroupEval> groupEvals, List<EvalLine> evalLines)
        {
            // 1) Prefer configured denial reasons from ALL failed groups (top-down order).
            try
            {
                var denialReasons = CollectDenialReasons(groupEvals ?? new List<GroupEval>());

                // Preserve order, remove duplicates (case-insensitive)
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var all = new List<string>();

                foreach (var dr in denialReasons)
                {
                    var r = (dr?.reason ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(r)) continue;

                    if (seen.Add(r))
                        all.Add(r);
                }

                if (all.Count > 0)
                {
                    var joined = string.Join(", ", all);

                    // Safety trim (adjust if your field allows more)
                    if (joined.Length > 4000) joined = joined.Substring(0, 4000);
                    return joined;
                }
            }
            catch
            {
                // ignore - fallback below
            }

            // 2) Fallback to first failed condition line (same behavior as before)
            var firstFail = (evalLines ?? new List<EvalLine>()).FirstOrDefault(l => l != null && !l.pass);
            if (firstFail != null)
            {
                var label = string.IsNullOrWhiteSpace(firstFail.label) ? "Eligibility criteria" : firstFail.label.Trim();
                var expected = FormatValueForComment(firstFail.expected);
                var actual = FormatValueForComment(firstFail.actual);

                if (!string.IsNullOrWhiteSpace(expected) || !string.IsNullOrWhiteSpace(actual))
                    return $"{label} (Expected: {expected}; Current: {actual})";

                return label;
            }

            return "Eligibility criteria failed.";
        }


        private static string GetPrimaryDenialReasonText(List<GroupEval> groupEvals, List<EvalLine> evalLines)
        {
            // 1) Prefer configured denial reason from first failed group (top-down order).
            try
            {
                var denialReasons = CollectDenialReasons(groupEvals);
                var primary = (denialReasons != null) ? denialReasons.FirstOrDefault() : null;
                if (primary != null && !string.IsNullOrWhiteSpace(primary.reason))
                {
                    return primary.reason.Trim();
                }
            }
            catch { /* ignore */ }

            // 2) Fallback to first failed condition line.
            var firstFail = (evalLines ?? new List<EvalLine>()).FirstOrDefault(l => l != null && !l.pass);
            if (firstFail != null)
            {
                var label = string.IsNullOrWhiteSpace(firstFail.label) ? "Eligibility criteria" : firstFail.label.Trim();
                var expected = FormatValueForComment(firstFail.expected);
                var actual = FormatValueForComment(firstFail.actual);

                if (!string.IsNullOrWhiteSpace(expected) || !string.IsNullOrWhiteSpace(actual))
                    return $"{label} (Expected: {expected}; Current: {actual})";

                return label;
            }

            return "Eligibility criteria failed.";
        }

        private static string FormatValueForComment(object val)
        {
            if (val == null) return "";
            if (val is bool b) return b ? "Yes" : "No";
            if (val is Money m) return m.Value.ToString("0.##", CultureInfo.InvariantCulture);
            if (val is DateTime dt) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return val.ToString();
        }


        #region ====== Rule 1 token population (Income + Expense only) ======

        private static void PopulateRule1Tokens(IOrganizationService svc, ITracingService tracing, Guid caseId, Dictionary<string, object> tokens)
        {
            tokens["applicableincome"] = HasActiveApplicableIncome(svc, tracing, caseId);
            tokens["applicableexpense"] = HasActiveApplicableExpense(svc, tracing, caseId);

            tracing.Trace($"Rule1 Tokens => applicableincome={tokens["applicableincome"]}, applicableexpense={tokens["applicableexpense"]}");
        }

        // Robust Yes check: supports Two Options + Choice (uses FormattedValues "Yes"/"No")
        private static bool IsYes(Entity row, string attributeLogicalName)
        {
            if (row == null) return false;
            if (!row.Attributes.Contains(attributeLogicalName) || row[attributeLogicalName] == null) return false;

            // Best: FormattedValue for choice/two-options
            if (row.FormattedValues != null && row.FormattedValues.ContainsKey(attributeLogicalName))
            {
                var fmt = (row.FormattedValues[attributeLogicalName] ?? "").Trim();
                if (fmt.Equals("Yes", StringComparison.OrdinalIgnoreCase)) return true;
                if (fmt.Equals("No", StringComparison.OrdinalIgnoreCase)) return false;
            }

            var v = row[attributeLogicalName];

            if (v is bool b) return b;

            if (v is OptionSetValue os)
            {
                // fallback (most environments Yes=1) - formatted value above is preferred
                return os.Value == 1;
            }

            if (v is int i) return i == 1;
            if (v is long l) return l == 1;

            var s = v.ToString();
            return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s == "1";
        }

        private static bool HasActiveApplicableIncome(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            var qe = new QueryExpression(ENT_CaseIncome)
            {
                ColumnSet = new ColumnSet("mcg_caseincomeid", FLD_CI_ApplicableIncome, "statecode"),
                TopCount = 50
            };

            qe.Criteria.AddCondition(FLD_CI_Case, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var rows = svc.RetrieveMultiple(qe).Entities;
            var found = rows.Any(r => IsYes(r, FLD_CI_ApplicableIncome));

            tracing.Trace($"HasActiveApplicableIncome(caseId={caseId}) rows={rows.Count} => {found}");
            return found;
        }

        private static bool HasActiveApplicableExpense(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            var qe = new QueryExpression(ENT_CaseExpense)
            {
                ColumnSet = new ColumnSet("mcg_caseexpenseid", FLD_CI_ApplicableIncome, "statecode"),
                TopCount = 50
            };

            qe.Criteria.AddCondition(FLD_Common_Case, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var rows = svc.RetrieveMultiple(qe).Entities;
            var found = rows.Any(r => IsYes(r, FLD_CI_ApplicableIncome));

            tracing.Trace($"HasActiveApplicableExpense(caseId={caseId}) rows={rows.Count} => {found}");
            return found;
        }

        #endregion

        #region ====== Rule 7 token population (State CCS Eibility) ======
        //Rule 7 token population (State CCS Eibility)
        private static void PopulateRule7Tokens(IPluginExecutionContext context, IOrganizationService svc, ITracingService tracing, Guid caseId, Dictionary<string, object> tokens)
        {
            tracing.Trace("PopulateRule7Tokens Method is called (with program specific deductions)");

            var bliId = GetGuidFromInput(context, IN_CaseBenefitLineItemId);

            var netAfter = ApplyProgramSpecificDeductionsAndUpdateCase(
                svc,
                tracing,
                caseId,
                bliId,
                out var totalDeductions,
                out var relativeKidsCount,
                out var medicalDeductionApplied,
                out var selfEmployedCount
            );

            tokens["yearlyincome"] = netAfter;
            tokens["householdsizeadjusted"] = CountHouseHoldSize(svc, tracing, caseId);

            // Compare netAfter (NOT mcg_yearlyhouseholdincome)
            //var bliId = GetGuidFromInput(context, IN_CaseBenefitLineItemId);

            decimal minIncome;
            bool incomeOk = IsIncomeBelowMinForStateCcs(svc, tracing, caseId, bliId, netAfter, out minIncome);

            bool paystubPresent, w2Present;
            bool docOk = HasIncomeDocForStateCcs(svc, tracing, caseId, out paystubPresent, out w2Present);

            // New tokens (use these in rule JSON so UI shows both reasons)
            tokens["incomerangematched"] = incomeOk;        // income below threshold
            tokens["hasincomedocument"] = docOk;          // paystub OR W2 present

            // Optional: expose details if you want to show “Current: …” nicely
            tokens["incomerange_min"] = minIncome;
            tokens["income_paystub_present"] = paystubPresent;
            tokens["income_w2_present"] = w2Present;

            // Backward compatibility: keep old token but make it explicit
            tokens["incomewithinrange"] = incomeOk && docOk;


        }


        private static decimal YearlyHouseHoldIncome(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            tracing.Trace($"YearlyHouseHoldIncome Method is called");
            var qe = new QueryExpression(ENT_Case)
            {
                ColumnSet = new ColumnSet(FLD_CASE_YearlyHouseholdIncome, "statecode"),
                TopCount = 50
            };

            qe.Criteria.AddCondition(FLD_CASE_IncidentId, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);


            var caseEntity = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();

            if (caseEntity == null || !caseEntity.Contains(FLD_CASE_YearlyHouseholdIncome))
            {
                tracing.Trace("YearlyHouseHoldIncome: No value found, returning 0");
                tracing.Trace($"YearlyHouseHoldIncome Method is end");
                return 0;
            }

            var money = caseEntity.GetAttributeValue<Money>(FLD_CASE_YearlyHouseholdIncome);
            var value = money?.Value ?? 0;

            tracing.Trace($"YearlyHouseHoldIncome(caseId={caseId}) = {value}");
            tracing.Trace($"YearlyHouseHoldIncome Method is end");
            return value;
        }

        private static decimal CountHouseHoldSize(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            tracing.Trace($"CountHouseHoldSize Method is called");

            var houseHoldSize = GetActiveHouseholdCount(svc, tracing, caseId);
            tracing.Trace($"CountHouseHoldSize is: {houseHoldSize.Count} ");
            tracing.Trace($"CountHouseHoldSize Method is end");
            return houseHoldSize.Count;
        }

        private static bool IsIncomeBelowMinForStateCcs(
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId,
    Guid bliId,
    decimal incomeToCheck,
    out decimal minIncomeUsed)
        {
            minIncomeUsed = 0m;

            try
            {
                var householdSize = (int)CountHouseHoldSize(svc, tracing, caseId);

                var bli = svc.Retrieve(ENT_BenefitLineItem, bliId, new ColumnSet(FLD_BLI_Benefit));
                var benefitRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_Benefit);

                if (benefitRef == null || string.IsNullOrWhiteSpace(benefitRef.Name))
                {
                    tracing.Trace("[Rule7] Benefit name missing.");
                    return false;
                }

                var serviceBenefitName = benefitRef.Name.Trim();

                // Eligibility Admin by name
                var eaQuery = new QueryExpression(ENT_EligibilityAdmin)
                {
                    ColumnSet = new ColumnSet(FLD_EA_Name),
                    TopCount = 1
                };
                eaQuery.Criteria.AddCondition(FLD_EA_Name, ConditionOperator.Equal, serviceBenefitName);

                var admin = svc.RetrieveMultiple(eaQuery).Entities.FirstOrDefault();
                if (admin == null)
                {
                    tracing.Trace($"[Rule7] No Eligibility Admin found for '{serviceBenefitName}'.");
                    return false;
                }

                // Income range records for subsidy 'c'
                var rangeQuery = new QueryExpression(ENT_EligibilityIncomeRange)
                {
                    ColumnSet = new ColumnSet(FLD_EIR_HouseHoldSize, FLD_EIR_MinIncome, ENT_SubsidyTableName),
                    TopCount = 5000
                };
                rangeQuery.Criteria.AddCondition(FLD_EIR_EligibilityAdmin, ConditionOperator.Equal, admin.Id);
                rangeQuery.Criteria.AddCondition(ENT_SubsidyTableName, ConditionOperator.Equal, "c");

                var ranges = svc.RetrieveMultiple(rangeQuery).Entities;

                var matched = ranges
                    .Where(r =>
                        r.GetAttributeValue<int?>(FLD_EIR_HouseHoldSize) == householdSize &&
                        string.Equals((r.GetAttributeValue<string>(ENT_SubsidyTableName) ?? "").Trim(), "c", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!matched.Any())
                {
                    tracing.Trace($"[Rule7] No income range matched for HouseholdSize={householdSize} (subsidy=c).");
                    return false;
                }

                // Usually one record exists; if multiple, use the smallest minIncome (safest)
                minIncomeUsed = matched
                    .Select(r => r.GetAttributeValue<Money>(FLD_EIR_MinIncome)?.Value ?? 0m)
                    .DefaultIfEmpty(0m)
                    .Min();

                bool incomeOk = incomeToCheck < minIncomeUsed;

                tracing.Trace($"[Rule7] IncomeCheck: income={incomeToCheck}, min={minIncomeUsed}, pass={incomeOk}");
                return incomeOk;
            }
            catch (Exception ex)
            {
                tracing.Trace("[Rule7] IsIncomeBelowMinForStateCcs ERROR: " + ex);
                return false;
            }
        }

        private static bool HasIncomeDocForStateCcs(
            IOrganizationService svc,
            ITracingService tracing,
            Guid caseId,
            out bool paystubPresent,
            out bool w2Present)
        {
            paystubPresent = false;
            w2Present = false;

            try
            {
                // same logic as your single plugin intent: Paystub OR W2 is enough
                paystubPresent = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Income, DocumentSubCategory.Paystub);
                w2Present = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Income, DocumentSubCategory.W2);

                bool docOk = paystubPresent || w2Present;

                tracing.Trace($"[Rule7] IncomeDocs: paystub={paystubPresent}, w2={w2Present}, pass={docOk}");
                return docOk;
            }
            catch (Exception ex)
            {
                tracing.Trace("[Rule7] HasIncomeDocForStateCcs ERROR: " + ex);
                return false;
            }
        }


        private static bool HasCheckEligibleIncomeRange(IPluginExecutionContext context, IOrganizationService svc, ITracingService tracing, Guid caseId, decimal incomeToCheck)
        {
            tracing.Trace("HasCheckEligibleIncomeRange method is started");

            var yearlyHouseHoldIncome = incomeToCheck;
            tracing.Trace($"Income To Check (After Deductions) = {yearlyHouseHoldIncome}");
           


            var caseHouseHoldSize = CountHouseHoldSize(svc, tracing, caseId);
            tracing.Trace($"Case Household Size = {caseHouseHoldSize}");

            var bliId = GetGuidFromInput(context, IN_CaseBenefitLineItemId);

            var bli = svc.Retrieve(
                ENT_BenefitLineItem,
                bliId,
                new ColumnSet(FLD_BLI_Benefit)
            );

            var serviceBenefitRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_Benefit);

            if (serviceBenefitRef == null || string.IsNullOrWhiteSpace(serviceBenefitRef.Name))
            {
                tracing.Trace("Service Benefit Name is NULL or empty");
                return false;
            }

            var serviceBenefitName = serviceBenefitRef.Name;
            tracing.Trace($"Service Benefit Name = {serviceBenefitName}");

            //  Get eligibility admin by name
            var eaQuery = new QueryExpression(ENT_EligibilityAdmin)
            {
                ColumnSet = new ColumnSet(FLD_EA_Name),
                TopCount = 1
            };

            eaQuery.Criteria.AddCondition(
                FLD_EA_Name,
                ConditionOperator.Equal,
                serviceBenefitName
            );

            var eligibilityAdmin =
                svc.RetrieveMultiple(eaQuery).Entities.FirstOrDefault();

            if (eligibilityAdmin == null)
            {
                tracing.Trace("No Eligibility Admin record found");
                return false;
            }

            var eligibilityAdminId = eligibilityAdmin.Id;
            tracing.Trace($"Eligibility Admin Id = {eligibilityAdminId}");

            // Get eligibility income range 
            var rangeQuery = new QueryExpression(ENT_EligibilityIncomeRange)
            {
                ColumnSet = new ColumnSet(
                    FLD_EIR_HouseHoldSize,
                    FLD_EIR_MinIncome,
                    ENT_SubsidyTableName
                )
            };

            rangeQuery.Criteria.AddCondition(
                FLD_EIR_EligibilityAdmin,
                ConditionOperator.Equal,
                eligibilityAdminId
            );

            rangeQuery.Criteria.AddCondition(
             ENT_SubsidyTableName,
            ConditionOperator.Equal,
            "c"
            );

            var ranges = svc.RetrieveMultiple(rangeQuery).Entities;

            if (!ranges.Any())
            {
                tracing.Trace("No Eligibility Income Range records found");
                return false;
            }

            tracing.Trace($"Total Income Range Records = {ranges.Count}");

            // Match household size & income
            var matchedRanges = ranges
                .Where(r =>
                    r.Contains(FLD_EIR_HouseHoldSize) &&
                    r.GetAttributeValue<int>(FLD_EIR_HouseHoldSize) == caseHouseHoldSize &&
                    string.Equals(r.GetAttributeValue<string>(ENT_SubsidyTableName)?.Trim() ?? "", "c",
                     StringComparison.OrdinalIgnoreCase
                     )
                )
                .ToList();

            if (!matchedRanges.Any())
            {
                tracing.Trace("No income range matched for Household Size");
                return false;
            }

            tracing.Trace($"Matched income range count = {matchedRanges.Count}");

            foreach (var range in matchedRanges)
            {
                var minIncomeMoney =
                    range.GetAttributeValue<Money>(FLD_EIR_MinIncome);

                var minIncome = minIncomeMoney?.Value ?? 0;

                tracing.Trace($"Comparing yearlyHouseHoldIncome={yearlyHouseHoldIncome} with MinIncome={minIncome}");
                // not eligible
                if (yearlyHouseHoldIncome >= minIncome)
                {
                    tracing.Trace(
                        "Yearly Eligible Income >= MinIncome : NOT ELIGIBLE");
                    return false;
                }
            }
            bool incomePayStubPresent = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Income, DocumentSubCategory.Paystub);
            bool incomeW2FPresent = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Income, DocumentSubCategory.W2);
            var finalResult = incomePayStubPresent || incomeW2FPresent;
            tracing.Trace($"incomePayStubPresent = {incomePayStubPresent}");
            tracing.Trace($"incomeW2FPresent = {incomeW2FPresent}");
            // eligible
            tracing.Trace("Yearly Eligible Income < MinIncome : ELIGIBLE");
            tracing.Trace("HasCheckEligibleIncomeRange method is end");

            return finalResult;
        }

        #endregion

        #region ====== Rule 8 token population (Child support, court ordered or voluntary child support) ======
        private static void PopulateRule8Tokens(IPluginExecutionContext context, IOrganizationService svc, ITracingService tracing, Guid caseId, Dictionary<string, object> tokens)
        {
            tracing.Trace($"PopulateRule8Tokens Method is called");
            SetToken(tokens, "ncpexists", CheckNcpExistsForChild(svc, tracing, caseId));
            SetToken(tokens, "ncppayschildsupport", ValidateChildSupport(svc, tracing, caseId));


            tokens["otheradultpartnerorspouse"] = GetActiveHouseholdCount(svc, tracing, caseId, CaseRelationShipLookup.SpouseOrPartner).Any();

            tokens["issingleparent"] = CheckMaritalStatusAllowedFromCaseContact(
              svc,
              tracing,
              caseId,
              ContactMaritalStatus.SingleOrNeverMarried,
              ContactMaritalStatus.Divorced,
              ContactMaritalStatus.Separated
            );

            tracing.Trace($"Rule8 Tokens completed");
            tracing.Trace($"PopulateRule8Tokens Method is end");
        }

        private static bool CheckNcpExistsForChild(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            tracing.Trace("CheckNcpExistsForChild check started");
            var caseRelationship = new List<InvolvedPartiesRelationship>
                {
                    InvolvedPartiesRelationship.SpouseOrPartner,
                    InvolvedPartiesRelationship.OtherParent,
                    InvolvedPartiesRelationship.Parent,
                    InvolvedPartiesRelationship.OtherFamilyMember
                };
            var caseInvolvedPartRecord = CheckCaseInvolvedParties(svc, tracing, caseId, caseRelationship.ToArray());
            bool isInvolvedPartiesPresent = caseInvolvedPartRecord.Any();

            var houseHoldNcpRecord = GetHouseHoldNcpMembers(svc, tracing, caseId);
            bool isCaseHouseHoldNcpPresent = houseHoldNcpRecord.Any();

            tracing.Trace($"caseInvolvedPartCheck {isInvolvedPartiesPresent}");
            tracing.Trace($"isActiveCaseHouseHoldPresent {isCaseHouseHoldNcpPresent}");

            tracing.Trace("CheckNcpExistsForChild check ended");
            return isInvolvedPartiesPresent || isCaseHouseHoldNcpPresent;
        }
        private static bool ValidateChildSupport(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            bool isChildCaseIncomePresent = HasAnyCaseIncome(svc, tracing, caseId, new[] { CaseIncomeCatergory.Other }, new[] { CaseIncomeSubCatergory.ChildSupport });
            tracing.Trace($"isChildCaseIncomePresent{isChildCaseIncomePresent}");
            bool hasChildSupportDocPresent = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.ChildSupport);
            tracing.Trace($"hasChildSupportDocPresent{hasChildSupportDocPresent}");
            return hasChildSupportDocPresent && isChildCaseIncomePresent;
        }
        #endregion

        #region ====== Rule 9 token population (Medical Expense calculation) ======
        //Rule 9 token population (Medical Expense >= 2500)
        private static void PopulateRule9Tokens(IPluginExecutionContext context, IOrganizationService svc, ITracingService tracing, Guid caseId, Dictionary<string, object> tokens)
        {
            tracing.Trace($"PopulateRule9Tokens Method is called");
            tokens["medicalbillsamount"] = CalculateMedicalExpense(svc, tracing, caseId);
            //tokens["medicalbillexists"] = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.Expense);
            bool hashospitalbills = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.HospitalBill);
            bool hasmedication = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.MedicationCosts);
            bool hasmedicalreceipt = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.MedicalReceipts);
            bool finalresult = hashospitalbills || hasmedication || hasmedicalreceipt;
            tokens["medicalbillexists"] = finalresult;

            tracing.Trace($"Rule9 Tokens => medicalbillsamount={tokens["medicalbillsamount"]}");
            tracing.Trace($"PopulateRule9Tokens Method is end");
        }

        private static decimal CalculateMedicalExpense(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            tracing.Trace("CalculateMedicalExpense method started");

            decimal totalAmount = 0;

            var qe = new QueryExpression(ENT_CaseExpense)
            {
                ColumnSet = new ColumnSet(FLD_CE_ExpenseType, FLD_CE_Amount),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
            {
                new ConditionExpression(FLD_Common_Case, ConditionOperator.Equal, caseId),
                new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                new ConditionExpression(
                    FLD_CE_ExpenseType,
                    ConditionOperator.In,
                    new object[]
                    {
                        (int)CaseExpenseType.MedicalBills,
                        (int)CaseExpenseType.MedicalPremiumExcludingMedicare,
                        (int)CaseExpenseType.MedicarePremium
                    })
            }
                }
            };

            var expenses = svc.RetrieveMultiple(qe)?.Entities;

            if (expenses == null || expenses.Count == 0)
            {
                tracing.Trace("No medical expense records found");
                return 0;
            }

            foreach (var expense in expenses)
            {
                var money = expense.GetAttributeValue<Money>(FLD_CE_Amount);
                if (money != null)
                {
                    totalAmount += money.Value;
                }
            }
            ;

            tracing.Trace($"Total Medical Expense (caseId={caseId}) = {totalAmount}");
            tracing.Trace("CalculateMedicalExpense method ended");

            return totalAmount;
        }
        #endregion

        #region ====== Rule 10 token population(Medical Expense calculation) ======
        //Rule 10 token population (single-parent household or has an absent parent)
        private static void PopulateRule10Tokens(IPluginExecutionContext context, IOrganizationService svc, ITracingService tracing, Guid caseId, Dictionary<string, object> tokens)
        {
            tracing.Trace($"PopulateRule10Tokens Method is called");
            tokens["singleparentfamily"] = CheckHouseHoldPartnerAndMaritalStatus(svc, tracing, caseId);
            tokens["childsupportdocumentprovided"] = HasDocumentByCategorySubcategory(svc, tracing, caseId, null, DocumentCategory.Expenses, DocumentSubCategory.ChildSupport);

            tracing.Trace($"Rule10 Tokens => singleparentfamily={tokens["singleparentfamily"]},childsupportdocumentprovided={tokens["childsupportdocumentprovided"]}");
            tracing.Trace($"PopulateRule10Tokens Method is end");
        }


        private static bool CheckHouseHoldPartnerAndMaritalStatus(IOrganizationService svc, ITracingService tracing, Guid caseId)
        {
            tracing.Trace("CheckHouseHoldPartnerAndMaritalStatus check started");

            bool hasHouseholdPartner = GetActiveHouseholdCount(
                svc,
                tracing,
                caseId,
                CaseRelationShipLookup.SpouseOrPartner,
                CaseRelationShipLookup.DomesticPartner,
                CaseRelationShipLookup.OtherParent,
                CaseRelationShipLookup.Partner
            ).Any();

            tracing.Trace($"Household partner exists: {hasHouseholdPartner}");

            bool validateMaritalStatus = CheckMaritalStatusAllowedFromCaseContact(
              svc,
              tracing,
              caseId,
              ContactMaritalStatus.Single,
              ContactMaritalStatus.Divorced,
              ContactMaritalStatus.Separated
          );

            // returning a single bool value
            bool result = hasHouseholdPartner && validateMaritalStatus;

            tracing.Trace($"CheckHouseHoldPartnerAndMaritalStatus final result: {result}");
            tracing.Trace("CheckHouseHoldPartnerAndMaritalStatus method end");
            return result;
        }


        #endregion

        #region ====== Household ======
        private static List<Entity> GetActiveHouseholdCount(IOrganizationService svc, ITracingService tracing, Guid caseId, params string[] relationships)
        {
            tracing.Trace($"GetActiveHouseholdCount Method is called");
            var qe = new QueryExpression(ENT_CaseHousehold)
            {
                ColumnSet = new ColumnSet(
                    FLD_CH_Contact, FLD_CH_DateEntered, FLD_CH_DateExited, FLD_CH_Primary, FLD_CH_StateCode, FLD_CH_RelationshipRole
                )
            };

            qe.Criteria.AddCondition(FLD_CH_Case, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_CH_StateCode, ConditionOperator.Equal, 0);
            qe.Criteria.AddCondition(FLD_CH_DateExited, ConditionOperator.Null);

            if (relationships != null && relationships.Length > 0)
            {
                var roleLink = qe.AddLink(
                    ENT_RelationshipRole,     // target table
                    FLD_CH_RelationshipRole,     // lookup field in CaseHousehold
                    FLD_RR_RRID,   // PK of target
                    JoinOperator.Inner
                );

                roleLink.EntityAlias = "rr";

                roleLink.LinkCriteria.AddCondition(
                    FLD_RR_Name,
                    ConditionOperator.In,
                    relationships.Cast<object>().ToArray()
                );
            }

            var results = svc.RetrieveMultiple(qe).Entities.ToList();
            tracing.Trace($"GetActiveHouseholdCount count: {results.Count}");
            tracing.Trace($"GetActiveHouseholdCount Method is end");
            return results;
        }

        //Helper method for Household NCP check
        private static List<Entity> GetHouseHoldNcpMembers(IOrganizationService svc, ITracingService tracing, Guid caseId, params string[] relationships)
        {
            tracing.Trace($"GetHouseHoldNcpMembers Method is called");
            var qe = new QueryExpression(ENT_CaseHousehold)
            {
                ColumnSet = new ColumnSet(
                    FLD_CH_Contact, FLD_CH_DateEntered, FLD_CH_DateExited, FLD_CH_Primary, FLD_CH_StateCode, FLD_CH_RelationshipRole
                )
            };

            qe.Criteria.AddCondition(FLD_CH_Case, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_CH_StateCode, ConditionOperator.Equal, 0);
            qe.Criteria.AddCondition(FLD_CH_DateExited, ConditionOperator.NotNull);

            if (relationships != null && relationships.Length > 0)
            {
                var roleLink = qe.AddLink(
                    ENT_RelationshipRole,     // target table
                    FLD_CH_RelationshipRole,     // lookup field in CaseHousehold
                    FLD_RR_RRID,   // PK of target
                    JoinOperator.Inner
                );

                roleLink.EntityAlias = "rr";

                roleLink.LinkCriteria.AddCondition(
                    FLD_RR_Name,
                    ConditionOperator.In,
                    relationships.Cast<object>().ToArray()
                );
            }

            var results = svc.RetrieveMultiple(qe).Entities.ToList();

            var matchedRecords = results
                .Where(e =>
                    e.Contains(FLD_CH_DateEntered) &&
                    e.Contains(FLD_CH_DateExited) &&
                    e.GetAttributeValue<DateTime>(FLD_CH_DateEntered).Day ==
                    e.GetAttributeValue<DateTime>(FLD_CH_DateExited).Day &&
                    e.GetAttributeValue<DateTime>(FLD_CH_DateEntered).Month ==
                    e.GetAttributeValue<DateTime>(FLD_CH_DateExited).Month
                )
                .ToList();

            tracing.Trace($"GetHouseHoldNcpMembers matchedRecords: {matchedRecords.Any()}");
            tracing.Trace($"GetHouseHoldNcpMembers count: {results.Count}");
            tracing.Trace($"GetHouseHoldNcpMembers Method is end");
            return results;
        }
        #endregion

        #region ====== Rule 2 token population (WPA Activity Hours) ======

        /// <summary>
        /// Rule 2 (WPA Activity):
        /// Beneficiary -> Contact Association (mcg_relationship) -> Father/Mother -> related parent contact
        /// Parent total hours/week = SUM(mcg_income.mcg_workhours) + SUM(mcg_educationdetails.mcg_workhours)
        /// Pass if EACH parent found (Father/Mother) has total >= 25
        /// </summary>
        /// <summary>
        /// Rule 2 (WPA Activity):
        /// Beneficiary -> Contact Association (mcg_relationship) -> Father/Mother -> related parent contact
        ///
        /// For EACH parent found:
        ///   Employment hours/week = SUM(mcg_income.mcg_workhours)  [all active income rows for that parent]
        ///   Education hours/week  = SUM(mcg_educationdetails.mcg_workhours)  [all active education rows for that parent]
        ///   Total activity hours/week = employment + education
        ///
        /// WPA pass logic (current requirement): EACH parent must have Total >= 25.
        ///
        /// Notes:
        /// - The rule JSON in PCF uses token: totalactivityhoursperweek >= 25
        ///   To represent "each parent meets 25", we set totalactivityhoursperweek = MIN(parentTotalHours).
        ///   Then (MIN >= 25) ? all parents >= 25.
        /// - We also add facts for UI summary (employment/education breakdown + per-parent totals).
        /// </summary>
        private static void PopulateRule2Tokens_WpaActivity(
            IOrganizationService svc,
            ITracingService tracing,
            Guid caseId,
            Guid beneficiaryContactId,
            Dictionary<string, object> tokens,
            Dictionary<string, object> facts)
        {
            var parentIds = GetActiveParentsForBeneficiary(svc, tracing, beneficiaryContactId);

            // If none found, safe default (rule fails)
            if (parentIds.Count == 0)
            {
                tokens[TOKEN_ActivityRequirementMet] = false;
                tokens[TOKEN_EvidenceCareNeededForChild] = false;
                tokens[TOKEN_Parent1ActivityHours] = 0m;
                tokens[TOKEN_Parent2ActivityHours] = 0m;
                tokens[TOKEN_HasEnrollmentDocument] = true;
                facts["wpa.enrollment.missingParents"] = "";

                // IMPORTANT: token used in rule JSON (min across parents; here none -> 0)
                tokens[TOKEN_ParentsTotalActivityHours] = 0m; // (this constant is totalactivityhoursperweek)

                facts["wpa.parents.count"] = 0;
                facts["wpa.activity.eachParentMeets25"] = false;
                facts["wpa.activity.employmentHoursPerWeekTotal"] = 0m;
                facts["wpa.activity.educationHoursPerWeekTotal"] = 0m;
                facts["wpa.activity.combinedHoursPerWeekTotal"] = 0m;

                tracing.Trace("Rule2: No Father/Mother parents found => activityrequirementmet=false, totalactivityhoursperweek=0");
                return;
            }

            // Parent-level totals
            var parentTotals = new List<decimal>();
            var parentWorkTotals = new List<decimal>();
            var parentEduTotals = new List<decimal>();
            var parentNames = new List<string>();

            decimal allWork = 0m;
            decimal allEdu = 0m;

            foreach (var pid in parentIds)
            {
                var work = SumIncomeWorkHoursPerWeek(svc, tracing, pid,caseId);
                var edu = SumEducationWorkHoursPerWeek(svc, tracing, pid);
                var total = work + edu;

                allWork += work;
                allEdu += edu;

                parentWorkTotals.Add(work);
                parentEduTotals.Add(edu);
                parentTotals.Add(total);

                // Name is only for UI friendliness; if missing, keep empty.
                var nm = TryGetContactFullName(svc, tracing, pid);
                parentNames.Add(nm ?? string.Empty);

                tracing.Trace($"Rule2: Parent={pid} Name='{nm}' WorkHours(sum mcg_income.mcg_workhours)={work}, EduHours(sum mcg_educationdetails.mcg_workhours)={edu}, Total={total}");
            }

            // Pass if each parent total >= 25
            bool eachParentMeets25 = parentTotals.All(h => h >= 25m);

            // Token used by some rule definitions: per-parent display
            tokens[TOKEN_ActivityRequirementMet] = eachParentMeets25;
            tokens[TOKEN_EvidenceCareNeededForChild] = eachParentMeets25;
            tokens[TOKEN_Parent1ActivityHours] = parentTotals.Count > 0 ? parentTotals[0] : 0m;
            tokens[TOKEN_Parent2ActivityHours] = parentTotals.Count > 1 ? parentTotals[1] : 0m;

            // Token used in WPA template JSON: totalactivityhoursperweek >= 25
            // To model "EACH parent must be >= 25", we store MIN(parentTotals)
            var minAcrossParents = parentTotals.Min();
            tokens[TOKEN_ParentsTotalActivityHours] = minAcrossParents;

            tokens["parentstotalactivityhoursperweek"] = allWork + allEdu; // backward-compatible alias

            // Facts for your HTML summary (case-worker friendly)
            facts["wpa.parents.count"] = parentIds.Count;
            facts["wpa.activity.eachParentMeets25"] = eachParentMeets25;

            facts["wpa.rule3.evidenceCareNeededForChild"] = eachParentMeets25;
            // Totals across parents
            facts["wpa.activity.employmentHoursPerWeekTotal"] = allWork;
            facts["wpa.activity.educationHoursPerWeekTotal"] = allEdu;
            facts["wpa.activity.combinedHoursPerWeekTotal"] = allWork + allEdu;

            // Min across parents (this is what drove the rule token)
            facts["wpa.activity.minTotalHoursPerWeekAcrossParents"] = minAcrossParents;

            // Per-parent breakdown (supports up to 2 parents for now, but keeps counts)
            if (parentTotals.Count > 0)
            {
                facts["wpa.parent1.name"] = parentNames[0];
                facts["wpa.parent1.employmentHoursPerWeek"] = parentWorkTotals[0];
                facts["wpa.parent1.educationHoursPerWeek"] = parentEduTotals[0];
                facts["wpa.parent1.totalHoursPerWeek"] = parentTotals[0];
            }
            if (parentTotals.Count > 1)
            {
                facts["wpa.parent2.name"] = parentNames[1];
                facts["wpa.parent2.employmentHoursPerWeek"] = parentWorkTotals[1];
                facts["wpa.parent2.educationHoursPerWeek"] = parentEduTotals[1];
                facts["wpa.parent2.totalHoursPerWeek"] = parentTotals[1];
            }

            // ===== NEW: Rule2 additional requirement - Proof of Enrollment document if education hours exist =====
            bool hasEnrollmentDocument = true;
            var missingEnrollmentParents = new List<string>();

            for (int i = 0; i < parentIds.Count; i++)
            {
                var parentId = parentIds[i];
                var eduHours = parentEduTotals[i];

                // Only require doc when this parent actually has education-hours filled (>0)
                if (eduHours > 0m)
                {
                    // Category can be "Verification" or "Verifications" (your code uses both in other rules)
                    bool docFound =
                        HasDocumentByCategorySubcategory(svc, tracing, caseId, parentId, "Verification", "Proof of Enrollment")
                        || HasDocumentByCategorySubcategory(svc, tracing, caseId, parentId, "Verifications", "Proof of Enrollment");

                    if (!docFound)
                    {
                        hasEnrollmentDocument = false;

                        var nm = (i < parentNames.Count ? parentNames[i] : "");
                        if (string.IsNullOrWhiteSpace(nm)) nm = parentId.ToString();

                        missingEnrollmentParents.Add(nm);
                    }
                }
            }

            // If NO parent has eduHours > 0 then missingEnrollmentParents stays empty and token stays true (auto-pass)
            tokens[TOKEN_HasEnrollmentDocument] = hasEnrollmentDocument;
            facts["wpa.enrollment.missingParents"] = string.Join(", ", missingEnrollmentParents);

            tracing.Trace($"Rule2 EnrollmentDoc Token => hasenrollmentdocument={hasEnrollmentDocument}; missingParents='{facts["wpa.enrollment.missingParents"]}'");


            tracing.Trace($"Rule2 Tokens => activityrequirementmet={eachParentMeets25}, parentCount={parentIds.Count}, minTotalAcrossParents(totalactivityhoursperweek)={minAcrossParents}, combinedAllParents={allWork + allEdu}");
        }

        private static string TryGetContactFullName(IOrganizationService svc, ITracingService tracing, Guid contactId)
        {
            try
            {
                var c = svc.Retrieve("contact", contactId, new ColumnSet("fullname"));
                return c.GetAttributeValue<string>("fullname");
            }
            catch (Exception ex)
            {
                tracing.Trace("TryGetContactFullName failed: " + ex.Message);
                return null;
            }
        }

        private static List<Guid> GetActiveParentsForBeneficiary(IOrganizationService svc, ITracingService tracing, Guid beneficiaryContactId)
        {
            var qe = new QueryExpression(ENT_ContactAssociation)
            {
                ColumnSet = new ColumnSet(
                    FLD_REL_RelatedContact,
                    FLD_REL_RoleType,
                    FLD_REL_EndDate,     // optional filter (kept)
                    FLD_REL_StateCode
                )
            };

            // Beneficiary -> association rows
            qe.Criteria.AddCondition(FLD_REL_Contact, ConditionOperator.Equal, beneficiaryContactId);

            // ? Only ACTIVE relationships
            qe.Criteria.AddCondition(FLD_REL_StateCode, ConditionOperator.Equal, 0);

            // Optional: EndDate is null OR EndDate >= today (keeps future-dated end valid)
            var today = DateTime.UtcNow.Date;
            var endFilter = new FilterExpression(LogicalOperator.Or);
            endFilter.AddCondition(FLD_REL_EndDate, ConditionOperator.Null);
            endFilter.AddCondition(FLD_REL_EndDate, ConditionOperator.OnOrAfter, today);
            qe.Criteria.AddFilter(endFilter);

            var rows = svc.RetrieveMultiple(qe).Entities;
            tracing.Trace($"Rule2: Active ContactAssociation rows for beneficiary={beneficiaryContactId} => {rows.Count}");

            var parents = new HashSet<Guid>();

            foreach (var row in rows)
            {
                // Relationship role display text must be Father/Mother
                if (!row.FormattedValues.TryGetValue(FLD_REL_RoleType, out var roleText) || string.IsNullOrWhiteSpace(roleText))
                    continue;

                roleText = roleText.Trim();

                if (!roleText.Equals("Father", StringComparison.OrdinalIgnoreCase) &&
                    !roleText.Equals("Mother", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var related = row.GetAttributeValue<EntityReference>(FLD_REL_RelatedContact);
                if (related == null || related.Id == Guid.Empty) continue;

                parents.Add(related.Id);
            }

            return parents.ToList();
        }

        private static List<SummaryFact> BuildEligibilitySummaryFacts(
    IOrganizationService svc,
    ITracingService tracing,
    Entity bli,
    Entity inc,
    Guid caseId,
    Guid beneficiaryId,
    decimal householdSize,
    string citizenshipTextIfKnown
)
        {
            var list = new List<SummaryFact>();
            var bliId = bli.Id;

            // 1) incident.mcg_yearlyhouseholdincome
            // Apply deductions and update Case.mcg_programspecificdeductions
            var netAfter = ApplyProgramSpecificDeductionsAndUpdateCase(
                svc,
                tracing,
                caseId,
                bliId,
                out var totalDeductions,
                out var relativeKidsCount,
                out var medicalDeductionApplied,
                out var selfEmployedCount
            );

            // BASE values from Case (already passed in as 'inc')
            var yearlyEligibleIncome = inc?.GetAttributeValue<Money>(FLD_CASE_YearlyEligibleIncome)?.Value ?? 0m;

            list.Add(new SummaryFact { label = "Household Net Income before deduction", value = "$" + yearlyEligibleIncome.ToString("0.##") });
            list.Add(new SummaryFact { label = "No of Relative Kids (Benefit Needed)", value = relativeKidsCount.ToString() });

            var selfEmpFlag = GetAnySelfEmployedFlag(svc, tracing, caseId);
            list.Add(new SummaryFact { label = "Self Employed", value = selfEmpFlag.HasValue ? (selfEmpFlag.Value ? "Yes" : "No") : "None" });

            list.Add(new SummaryFact { label = "Deduction Amount", value = "$" + totalDeductions.ToString("0.##") });

            var appliedParts = new List<string>();
            if (relativeKidsCount > 0) appliedParts.Add($"Relative Kids (${(relativeKidsCount * 5000m):0.##})");
            if (medicalDeductionApplied) appliedParts.Add("Medical Bills ($2500)");
            if (selfEmployedCount > 0) appliedParts.Add($"Self Employment ({selfEmployedCount} x 30%)");
            list.Add(new SummaryFact { label = "Deductions Applied", value = string.Join("; ", appliedParts) });

            list.Add(new SummaryFact { label = "Household Net Income after deduction", value = "$" + netAfter.ToString("0.##") });


            // 7) beneficiary name (recipient contact formatted is already on BLI lookup)
            var benRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_RecipientContact);
            list.Add(new SummaryFact { label = "Child Full Name", value = benRef?.Name ?? "" });

            // 8) Household size (from plugin)
            list.Add(new SummaryFact { label = "Household Size", value = householdSize.ToString("0") });

            // 9) Child Age (birthdate)
            string childAgeText = "";
            try
            {
                var ben = svc.Retrieve(ENT_ContactTableName, beneficiaryId, new ColumnSet("birthdate"));
                var dob = ben.GetAttributeValue<DateTime?>("birthdate");
                if (dob.HasValue)
                {
                    var today = DateTime.UtcNow.Date;
                    var age = today.Year - dob.Value.Date.Year;
                    if (dob.Value.Date > today.AddYears(-age)) age--;
                    childAgeText = age.ToString();
                }
            }
            catch { }
            list.Add(new SummaryFact { label = "Child Age", value = childAgeText });

            // 10) Child Disabled (mcg_disability yes/no)
            string disabledText = "";
            try
            {
                var ben = svc.Retrieve(ENT_ContactTableName, beneficiaryId, new ColumnSet("mcg_disability", "mcg_contactid"));
                if (ben.FormattedValues.ContainsKey("mcg_disability"))
                    disabledText = ben.FormattedValues["mcg_disability"];
                else if (ben.Attributes.Contains("mcg_disability") && ben["mcg_disability"] is bool b2)
                    disabledText = b2 ? "Yes" : "No";
            }
            catch { }
            list.Add(new SummaryFact { label = "Is Child Disabled", value = disabledText });

            // 11) Citizenship (reuse what you already derive; fallback)
            var citizenship = GetCitizenshipFromBirthCertificate(svc, tracing, beneficiaryId);
            list.Add(new SummaryFact
            {
                label = "Citizenship",
                value = string.IsNullOrWhiteSpace(citizenship) ? "Info. Not Provided" : citizenship
            });


            // 12) County (you said plugin already has it; if not present, keep blank)
            var county = GetCountyFromCaseAddress(svc, tracing, caseId);
            list.Add(new SummaryFact { label = "County", value = string.IsNullOrWhiteSpace(county) ? "Info. Not Provided" : county });

            // 13) Service name (mcg_servicebenefitnames formatted)
            var svcName = bli.GetAttributeValue<EntityReference>(FLD_BLI_Benefit)?.Name ?? "";
            list.Add(new SummaryFact { label = "Service Name", value = svcName });

            // 14) Benefit id formatted (lookup on BLI) - depends on your real field
            var benefitIdName = bli.GetAttributeValue<string>(FLD_BLI_BenefitId);
            list.Add(new SummaryFact { label = "Benefit Id", value = benefitIdName?? "" });

            // 15) EICM Contact Id (you said in beneficiary record a field exists; if you only want GUID, use beneficiaryId)
            var contactRecord = svc.Retrieve(ENT_ContactTableName, beneficiaryId, new ColumnSet("mcg_contactid"));
            string eicmContactId = contactRecord.Contains("mcg_contactid") ? contactRecord.GetAttributeValue<string>("mcg_contactid") : string.Empty;
            list.Add(new SummaryFact
            {
                label = "EICM Contact Id",
                value = string.IsNullOrWhiteSpace(eicmContactId)
        ? "Not Provided"
        : eicmContactId
            });

            // 16) Care level formatted
            list.Add(new SummaryFact { label = "Care Level", value = GetChoiceFormattedValue(bli, FLD_BLI_CareServiceLevel) });

            // 17) Care type formatted
            list.Add(new SummaryFact { label = "Care Type", value = GetChoiceFormattedValue(bli, FLD_BLI_CareServiceType) });

            // 18) Frequency formatted
            list.Add(new SummaryFact { label = "Benefit/Service Frequency", value = GetChoiceFormattedValue(bli, FLD_BLI_ServiceFrequency) });

            // 19) Client Verified formatted (mcg_verifiedids)
            list.Add(new SummaryFact { label = "Client Verified", value = GetChoiceFormattedValue(bli, FLD_BLI_Verified) });

            return list;
        }



        private static decimal SumIncomeWorkHoursPerWeek(IOrganizationService svc, ITracingService tracing, Guid parentContactId, Guid caseId)
        {
            // Step 1: find applicable Case Income rows for this case+parent
            var ciQ = new QueryExpression(ENT_CaseIncome)
            {
                ColumnSet = new ColumnSet(FLD_CI_ContactIncome),
                TopCount = 500
            };
            ciQ.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            ciQ.Criteria.AddCondition(FLD_CI_Case, ConditionOperator.Equal, caseId);
            ciQ.Criteria.AddCondition(FLD_CI_Contact, ConditionOperator.Equal, parentContactId);
            ciQ.Criteria.AddCondition(FLD_CI_ApplicableIncome, ConditionOperator.Equal, true);

            var caseIncomeRows = svc.RetrieveMultiple(ciQ).Entities;

            var incomeIds = caseIncomeRows
                .Select(x => x.GetAttributeValue<EntityReference>(FLD_CI_ContactIncome))
                .Where(r => r != null)
                .Select(r => r.Id)
                .Distinct()
                .ToList();

            if (incomeIds.Count == 0)
            {
                tracing.Trace($"Rule2: No applicable case income rows found. caseId={caseId}, parent={parentContactId}");
                return 0m;
            }

            // Step 2: load income rows and sum mcg_workhours
            var incQ = new QueryExpression(ENT_Income)
            {
                ColumnSet = new ColumnSet(FLD_INC_WorkHours),
                TopCount = 500
            };
            incQ.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            incQ.Criteria.AddCondition("mcg_incomeid", ConditionOperator.In, incomeIds.Cast<object>().ToArray());

            var rows = svc.RetrieveMultiple(incQ).Entities;
            decimal total = 0m;

            foreach (var r in rows)
            {
                total += ToDecimalSafe(r.Attributes.Contains(FLD_INC_WorkHours) ? r[FLD_INC_WorkHours] : null);
            }

            tracing.Trace($"Rule2: Applicable income rows={rows.Count} caseId={caseId} parent={parentContactId} totalWorkHours={total}");
            return total;
        }

        private static decimal SumEducationWorkHoursPerWeek(IOrganizationService svc, ITracingService tracing, Guid parentContactId)
        {
            var qe = new QueryExpression(ENT_EducationDetails)
            {
                ColumnSet = new ColumnSet(FLD_EDU_WorkHours),
                TopCount = 500
            };

            qe.Criteria.AddCondition(FLD_EDU_Contact, ConditionOperator.Equal, parentContactId);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var rows = svc.RetrieveMultiple(qe).Entities;
            decimal total = 0m;

            foreach (var r in rows)
            {
                total += ToDecimalSafe(r.Attributes.Contains(FLD_EDU_WorkHours) ? r[FLD_EDU_WorkHours] : null);
            }

            tracing.Trace($"Rule2: Education hours rows={rows.Count} parent={parentContactId} totalEduHours={total}");
            return total;
        }

        private static decimal ToDecimalSafe(object raw)
        {
            if (raw == null) return 0m;
            if (raw is decimal d) return d;
            if (raw is double db) return (decimal)db;
            if (raw is float f) return (decimal)f;
            if (raw is int i) return i;
            if (raw is long l) return l;
            if (raw is Money m) return m.Value;

            if (decimal.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            if (decimal.TryParse(raw.ToString(), out parsed))
                return parsed;

            return 0m;
        }

        #endregion

        #region ====== Household ======

        private List<Guid> GetActiveHouseholdIds(IOrganizationService service, ITracingService tracing, Guid caseId)
        {
            // Household table
            const string ENT_CASEHOUSEHOLD = "mcg_casehousehold";
            const string FLD_HH_CASE = "mcg_case";
            const string FLD_HH_CONTACT = "mcg_contact";
            const string FLD_STATECODE = "statecode"; // 0 = Active

            var ids = new List<Guid>();

            var qe = new Microsoft.Xrm.Sdk.Query.QueryExpression(ENT_CASEHOUSEHOLD)
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(FLD_HH_CONTACT),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression(Microsoft.Xrm.Sdk.Query.LogicalOperator.And)
            };

            qe.Criteria.AddCondition(FLD_HH_CASE, Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_STATECODE, Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, 0);

            var results = service.RetrieveMultiple(qe);

            foreach (var e in results.Entities)
            {
                if (e.Contains(FLD_HH_CONTACT) && e[FLD_HH_CONTACT] is EntityReference er && er.Id != Guid.Empty)
                {
                    ids.Add(er.Id);
                }
            }

            tracing.Trace($"[Rule4] Active household contacts found: {ids.Count}");
            return ids;
        }


        #endregion

        #region ====== Input / Result JSON ======

        private static Guid GetGuidFromInput(IPluginExecutionContext context, string paramName)
        {
            if (!context.InputParameters.Contains(paramName) || context.InputParameters[paramName] == null)
                throw new InvalidPluginExecutionException($"Missing required input parameter: {paramName}");

            var raw = context.InputParameters[paramName].ToString();
            if (!Guid.TryParse(raw, out var id))
                throw new InvalidPluginExecutionException($"Invalid GUID in parameter {paramName}: {raw}");

            return id;
        }

        private static List<DenialReasonLine> CollectDenialReasons(List<GroupEval> roots)
        {
            var list = new List<DenialReasonLine>();
            if (roots == null || roots.Count == 0) return list;

            foreach (var r in roots)
                Walk(r, parentPass: null, output: list);

            return list;
        }

        private static void Walk(GroupEval g, bool? parentPass, List<DenialReasonLine> output)
        {
            if (g == null) return;

            // include a group's denialReason if:
            // - this group FAILED
            // - and either it is a top-level group (parentPass == null)
            //   OR the parent also FAILED (parentPass == false)
            if (!g.pass && (parentPass == null || parentPass.Value == false))
            {
                var reason = (g.denialReason ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    output.Add(new DenialReasonLine
                    {
                        id = g.id,
                        label = g.label,
                        path = g.path,
                        reason = reason
                    });
                }
            }

            if (g.groups == null) return;
            foreach (var child in g.groups)
                Walk(child, g.pass, output);
        }


        private static string BuildResultJson(
    List<string> validationFailures,
    List<EvalLine> evaluationLines,
    List<CriteriaSummaryLine> criteriaSummary,
    List<string> parametersConsidered,
    bool isEligible,
    string resultMessage,
    Dictionary<string, object> facts = null,
    List<GroupEval> groupEvals = null,
    List<SummaryFact> summaryFacts = null)
        {

            var denialReasons = CollectDenialReasons(groupEvals ?? new List<GroupEval>());
            var primaryDenialReason = denialReasons.Count > 0 ? (denialReasons[0].reason ?? "") : "";

            var payload = new
            {
                validationFailures = validationFailures ?? new List<string>(),
                criteriaSummary = criteriaSummary ?? new List<CriteriaSummaryLine>(),
                parametersConsidered = parametersConsidered ?? new List<string>(),
                lines = evaluationLines ?? new List<EvalLine>(),
                groupEvals = groupEvals ?? new List<GroupEval>(),

                denialReasons = denialReasons,
                primaryDenialReason = primaryDenialReason,
                isEligible = isEligible,
                resultMessage = resultMessage ?? "",
                facts = facts ?? new Dictionary<string, object>(),
                summaryFacts = summaryFacts ?? new List<SummaryFact>()
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        }




        #endregion

        #region ====== Rule JSON + Evaluator ======

        private class RuleDefinition
        {
            public string @operator { get; set; } // "AND" | "OR"
            public List<RuleGroup> groups { get; set; }
        }

        private class RuleGroup
        {
            public string id { get; set; }
            public string label { get; set; }

            public string denialReason { get; set; }
            public string @operator { get; set; } // "AND" | "OR"
            public List<Condition> conditions { get; set; }
            public List<RuleGroup> groups { get; set; }
        }

        private class Condition
        {
            public string id { get; set; }
            public string token { get; set; }
            public string label { get; set; }
            public string @operator { get; set; }
            public object value { get; set; }
        }

        private class EvalLine
        {
            public string path { get; set; }
            public string token { get; set; }
            public string label { get; set; }
            public string op { get; set; }
            public object expected { get; set; }
            public object actual { get; set; }
            public bool pass { get; set; }
        }

        private class SummaryFact
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        private class CriteriaSummaryLine
        {
            public string id { get; set; }
            public string label { get; set; }
            public bool pass { get; set; }

            //public string denialReason { get; set; }   //Optional we can remove this 
        }

        private class DenialReasonLine
        {
            public string id { get; set; }
            public string label { get; set; }
            public string path { get; set; }
            public string reason { get; set; }
        }



        private static string GetTokenLabel(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return token;

            switch (token.Trim().ToLowerInvariant())
            {
                case "applicableincome": return "Applicable income present";
                case "applicableexpense": return "Applicable expense present";
                case "applicableasset": return "Applicable asset present";
                case "totalactivityhoursperweek": return "Total parent activity hours per week (work + education)";
                case "enrolledfulltimeprogram": return "Parent enrolled in a full-time program";
                case "evidencecareneededforchild": return "Evidence care is needed for the child";
                case "proofidentityprovided": return "Proof of identity provided for all household members";
                case "proofresidencyprovided": return "Proof of residency provided for all household members";
                case "mostrecenttaxreturnprovided": return "Most recent income tax return provided";
                case "pursuingchildsupportorgoodcause": return "Pursuing child support or good cause documented";
                case "childsupportdocumentprovided": return "Child support document provided";
                case "singleparentfamily": return "Is family a Single-parent family";
                case "absentparent": return "Absent parent";
                case "medicalbillsamount": return "Medical bills amount";
                case "yearlyincome": return "Yearly eligible income";
                case "householdsize": return "Household size";
                case "householdsizeadjusted": return "Household size (adjusted)";
                case "incomecategory": return "Income category (State A-J / C / D)";
                case "incomewithinrange": return "Income within eligible range";
                case "ncpexists": return "NCP Exists?";
                case "ncppayschildsupport": return "NCP Pay for Child Support?";
                case "otheradultpartnerorspouse": return "Any Adult Present?";
                case "issingleparent": return "Is a Single Parent?";
                case "medicalbillexists": return "Medical Bill Exists?";
                default: return token;
            }
        }

        private static List<CriteriaSummaryLine> EvaluateTopLevelGroups(
            RuleDefinition def,
            Dictionary<string, object> tokens,
            ITracingService tracing)
        {
            var summary = new List<CriteriaSummaryLine>();
            if (def?.groups == null) return summary;

            foreach (var g in def.groups)
            {
                bool groupPass = EvaluateGroup(g, tokens, tracing, new List<EvalLine>(), "ROOT");
                summary.Add(new CriteriaSummaryLine
                {
                    id = g.id,
                    label = g.label,
                    pass = groupPass
                });

                tracing.Trace($"CRITERIA SUMMARY: {g.id} '{g.label}' => {(groupPass ? "PASS" : "FAIL")}");
            }

            return summary;
        }

        private static List<string> BuildParametersConsideredForRule1(Dictionary<string, object> tokens)
        {
            string YesNo(object v)
            {
                if (v is bool b) return b ? "Yes" : "No";
                return (v?.ToString() ?? "");
            }

            return new List<string>
            {
                $"Applicable Income present (Active + Applicable) = {YesNo(tokens.ContainsKey("applicableincome") ? tokens["applicableincome"] : null)}",
                $"Applicable Expense present (Active + Applicable) = {YesNo(tokens.ContainsKey("applicableexpense") ? tokens["applicableexpense"] : null)}"
            };
        }

        private static RuleDefinition ParseRuleDefinition(string ruleJson)
        {
            if (string.IsNullOrWhiteSpace(ruleJson))
                return new RuleDefinition { @operator = "AND", groups = new List<RuleGroup>() };

            try
            {
                var def = JsonConvert.DeserializeObject<RuleDefinition>(ruleJson);
                if (def == null || def.groups == null) return new RuleDefinition { @operator = "AND", groups = new List<RuleGroup>() };
                if (string.IsNullOrWhiteSpace(def.@operator)) def.@operator = "AND";
                return def;
            }
            catch
            {
                return new RuleDefinition { @operator = "AND", groups = new List<RuleGroup>() };
            }
        }

        private static bool EvaluateRuleDefinition(
     RuleDefinition def,
     Dictionary<string, object> tokens,
     ITracingService tracing,
     List<EvalLine> lines,
     List<GroupEval> groupEvals)
        {
            var rootAnd = string.Equals(def.@operator, "AND", StringComparison.OrdinalIgnoreCase);

            var results = new List<bool>();

            foreach (var g in def.groups ?? new List<RuleGroup>())
            {
                var ge = EvaluateGroupTree(g, tokens, tracing, lines, "ROOT");
                groupEvals.Add(ge);
                results.Add(ge.pass);
            }

            return rootAnd ? results.All(x => x) : results.Any(x => x);
        }

        private static GroupEval EvaluateGroupTree(
    RuleGroup group,
    Dictionary<string, object> tokens,
    ITracingService tracing,
    List<EvalLine> flatLines,
    string parentPath)
        {
            var groupPath = $"{parentPath} > {(string.IsNullOrWhiteSpace(group.label) ? group.id : group.label)}";
            var isAnd = string.Equals(group.@operator, "AND", StringComparison.OrdinalIgnoreCase);

            var node = new GroupEval
            {
                id = group.id,
                label = string.IsNullOrWhiteSpace(group.label) ? group.id : group.label,
                path = groupPath,
                op = group.@operator,
                denialReason = (group.denialReason ?? "").Trim(),
            };

            var localResults = new List<bool>();

            // conditions
            foreach (var c in group.conditions ?? new List<Condition>())
            {
                var pass = EvaluateCondition(c, tokens, tracing, out var actual);
                localResults.Add(pass);

                var tokenKey = (c.token ?? "").Trim();

                var line = new EvalLine
                {
                    path = groupPath,
                    token = tokenKey,
                    label = GetTokenLabel(tokenKey),   // ? friendly label
                    op = c.@operator,
                    expected = c.value,
                    actual = actual,
                    pass = pass
                };

                node.conditions.Add(line);
                flatLines.Add(line); // keep your old output too (safe)
            }

            // children
            foreach (var child in group.groups ?? new List<RuleGroup>())
            {
                var childNode = EvaluateGroupTree(child, tokens, tracing, flatLines, groupPath);
                node.groups.Add(childNode);
                localResults.Add(childNode.pass);
            }

            node.pass = isAnd ? localResults.All(x => x) : localResults.Any(x => x);

            tracing.Trace($"GroupTree '{groupPath}' => {node.pass} (op={group.@operator})");
            return node;
        }


        private static bool EvaluateGroup(
    RuleGroup group,
    Dictionary<string, object> tokens,
    ITracingService tracing,
    List<EvalLine> lines,
    string parentPath)
        {
            var groupPath = $"{parentPath} > {(string.IsNullOrWhiteSpace(group.label) ? group.id : group.label)}";
            var isAnd = string.Equals(group.@operator, "AND", StringComparison.OrdinalIgnoreCase);

            var localResults = new List<bool>();

            foreach (var c in group.conditions ?? new List<Condition>())
            {
                var pass = EvaluateCondition(c, tokens, tracing, out var actual);
                localResults.Add(pass);
                var tokenKey = (c.token ?? "").Trim();

                lines.Add(new EvalLine
                {
                    path = groupPath,
                    token = tokenKey,
                    label = GetTokenLabel(tokenKey),   // ? key change
                    op = c.@operator,
                    expected = c.value,
                    actual = actual,
                    pass = pass
                });
            }

            foreach (var child in group.groups ?? new List<RuleGroup>())
                localResults.Add(EvaluateGroup(child, tokens, tracing, lines, groupPath));

            var result = isAnd ? localResults.All(x => x) : localResults.Any(x => x);
            tracing.Trace($"Group '{groupPath}' => {result} (op={group.@operator})");
            return result;
        }


        //private static string GetTokenLabel(xstring token)
        //{
        //    if (string.IsNullOrWhiteSpace(token)) return "";

        //    // ? Friendly labels for UI (fixes your Rule 3 "evidencecareneededforchild" showing raw token)
        //    switch (token.Trim().ToLowerInvariant())
        //    {
        //        case "evidencecareneededforchild":
        //            return "Evidence care is needed for the child";

        //        case "totalactivityhoursperweek":
        //            return "Total parent activity hours per week (work + education)";

        //        case "proofidentityprovided":
        //            return "Proof of identity provided for all household members";

        //        default:
        //            // fallback: show token but slightly readable
        //            return token;
        //    }
        //}



        private static int CalculateAge(DateTime dob)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age < 0 ? 0 : age;
        }

        private static bool? GetAnySelfEmployedFlag(IOrganizationService service, ITracingService tracing, Guid caseId)
        {
            const string ENT_CASEINCOME = "mcg_caseincome";
            const string FLD_CASE = "mcg_case";
            const string FLD_SELF = "mcg_selfemployed";
            const string FLD_STATE = "statecode";

            var qe = new QueryExpression(ENT_CASEINCOME)
            {
                ColumnSet = new ColumnSet(FLD_SELF),
                Criteria = new FilterExpression(LogicalOperator.And)
            };
            qe.Criteria.AddCondition(FLD_CASE, ConditionOperator.Equal, caseId);
            qe.Criteria.AddCondition(FLD_STATE, ConditionOperator.Equal, 0);
            qe.TopCount = 50;

            var res = service.RetrieveMultiple(qe);

            if (res.Entities.Count == 0)
            {
                tracing.Trace("Self employed: No income records found");
                return null;  // No records found
            }

            var any = res.Entities.Any(e => e.GetAttributeValue<bool?>(FLD_SELF) == true);
            tracing.Trace($"Self employed? = {any}");
            return any;
        }



        private static bool EvaluateCondition(Condition c, Dictionary<string, object> tokens, ITracingService tracing, out object actual)
        {
            var tokenKey = (c.token ?? "").Trim();
            tokens.TryGetValue(tokenKey, out actual);
            var op = (c.@operator ?? "").Trim().ToLowerInvariant();

            tracing.Trace($"TOKEN LOOKUP: '{tokenKey}' exists={tokens.ContainsKey(tokenKey)} actual={(actual == null ? "<null>" : actual.ToString())}");

            switch (op)
            {
                case "equals":
                case "=":
                    return AreEqual(actual, c.value);
                case "notequals":
                case "!=":
                    return !AreEqual(actual, c.value);
                case ">=":
                case "greaterorequal":
                    return CompareNumber(actual, c.value, (a, b) => a >= b);
                case "<=":
                case "lessorequal":
                    return CompareNumber(actual, c.value, (a, b) => a <= b);
                case ">":
                case "greaterthan":
                    return CompareNumber(actual, c.value, (a, b) => a > b);
                case "<":
                case "lessthan":
                    return CompareNumber(actual, c.value, (a, b) => a < b);
                default:
                    return false;
            }
        }

        private static bool AreEqual(object actual, object expected)
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;

            if (TryBool(actual, out var ab) && TryBool(expected, out var eb))
                return ab == eb;

            if (TryDecimal(actual, out var ad) && TryDecimal(expected, out var ed))
                return ad == ed;

            return string.Equals(actual.ToString(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        private class GroupEval
        {
            public string id { get; set; }
            public string label { get; set; }
            public string path { get; set; }
            public string op { get; set; }          // AND/OR
            public bool pass { get; set; }

            public string denialReason { get; set; }

            public List<EvalLine> conditions { get; set; } = new List<EvalLine>();
            public List<GroupEval> groups { get; set; } = new List<GroupEval>();
        }

        private static void SetToken(Dictionary<string, object> tokens, string key, object value)
        {
            if (tokens == null) return;
            var k = (key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(k)) return;

            tokens[k] = value;
        }


        private static bool CompareNumber(object actual, object expected, Func<decimal, decimal, bool> cmp)
        {
            if (!TryDecimal(actual, out var a)) return false;
            if (!TryDecimal(expected, out var b)) return false;
            return cmp(a, b);
        }

        private static bool TryDecimal(object v, out decimal d)
        {
            d = 0m;
            if (v == null) return false;

            if (v is decimal dd) { d = dd; return true; }
            if (v is double db) { d = (decimal)db; return true; }
            if (v is float f) { d = (decimal)f; return true; }
            if (v is int i) { d = i; return true; }
            if (v is long l) { d = l; return true; }
            if (v is Money m) { d = m.Value; return true; }

            return decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }

        private static bool TryBool(object v, out bool b)
        {
            b = false;
            if (v == null) return false;
            if (v is bool bb) { b = bb; return true; }
            if (v is string s && bool.TryParse(s, out var parsed)) { b = parsed; return true; }
            if (v is int i) { b = i != 0; return true; }
            if (v is long l) { b = l != 0; return true; }

            return false;
        }

        #endregion

        #region -- Create Notes Record --

        private static void TryCreateEligibilityNote(
    IOrganizationService svc,
    ITracingService tracing,
    Entity bli,
    EntityReference caseRef,
    EntityReference benefitRef,
    EntityReference recipientRef,
    bool isEligible,
    string resultMessage,
    List<string> validationFailures,
    List<EvalLine> evalLines,
    List<GroupEval> groupEvals,
    List<SummaryFact> summaryFacts)
        {
            try
            {

                var nowUtc = DateTime.UtcNow;

                var caseText = caseRef != null ? (!string.IsNullOrWhiteSpace(caseRef.Name) ? caseRef.Name : caseRef.Id.ToString()) : "";
                var benefitText = benefitRef != null ? (!string.IsNullOrWhiteSpace(benefitRef.Name) ? benefitRef.Name : benefitRef.Id.ToString()) : "";
                var childText = recipientRef != null ? (!string.IsNullOrWhiteSpace(recipientRef.Name) ? recipientRef.Name : recipientRef.Id.ToString()) : "";

                // Build plain text header for grid
                var headerText = BuildEligibilityHeaderText(
                    caseDisplay: caseText,     // e.g. "Created From SR : SR-K3V2G2-1018"
                    serviceBenefit: benefitText,               // e.g. "WPA 0-5 Child Care Services"
                    childName: childText,                         // e.g. "Lauren Roberts"
                    isEligible: isEligible,
                    utcNow: nowUtc
                );

                var html = BuildEligibilityNoteHtml(
                    bli,
                    caseRef,
                    benefitRef,
                    recipientRef,
                    isEligible,
                    resultMessage,
                    validationFailures,
                    evalLines,
                    groupEvals,
                    summaryFacts
                );

                var note = new Entity(ENT_EICMNotes);

                // Note Type = Eligibility (global choice)
                note[FLD_NOTE_Type] = new OptionSetValue(NOTE_TYPE_ELIGIBILITY);

                // Rich text field expects HTML
                note[FLD_NOTE_Description] = html;
                note[FLD_NOTE_DescriptionText] = headerText;

                note["mcg_name"] = $"Eligibility - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";

                // Link note to Case (lookup field: mcg_incident)
                if (caseRef != null)
                {
                    note[FLD_NOTE_CaseLookup] = caseRef; 
                }


                svc.Create(note);
                tracing.Trace("mcg_eicmnotes created for eligibility run.");
            }
            catch (Exception ex)
            {
                tracing.Trace("TryCreateEligibilityNote FAILED (ignored): " + ex);
                // Do not throw - note creation should never block eligibility
            }
        }

        private static string BuildEligibilityHeaderText(
    string caseDisplay,
    string serviceBenefit,
    string childName,
    bool isEligible,
    DateTime utcNow
)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Eligibility Determination");
            sb.AppendLine($"Date (UTC): {utcNow:yyyy-MM-dd HH:mm}");
            if (!string.IsNullOrWhiteSpace(caseDisplay)) sb.AppendLine($"Case: {caseDisplay}");
            if (!string.IsNullOrWhiteSpace(serviceBenefit)) sb.AppendLine($"Service/Benefit: {serviceBenefit}");
            if (!string.IsNullOrWhiteSpace(childName)) sb.AppendLine($"Child: {childName}");
            sb.AppendLine($"Result: {(isEligible ? "Eligible" : "Not Eligible")}");
            return sb.ToString().Trim();
        }


        private static string BuildEligibilityNoteHtml(
            Entity bli,
            EntityReference caseRef,
            EntityReference benefitRef,
            EntityReference recipientRef,
            bool isEligible,
            string resultMessage,
            List<string> validationFailures,
            List<EvalLine> evalLines,
            List<GroupEval> groupEvals,
            List<SummaryFact> summaryFacts)
        {
            string H(string s) => System.Security.SecurityElement.Escape(s ?? "");

            string F(object v)
            {
                if (v == null) return "";
                if (v is bool b) return b ? "Yes" : "No";
                if (v is decimal d) return d.ToString("0.##", CultureInfo.InvariantCulture);
                if (v is double db) return ((decimal)db).ToString("0.##", CultureInfo.InvariantCulture);
                if (v is float f) return ((decimal)f).ToString("0.##", CultureInfo.InvariantCulture);
                if (v is int i) return i.ToString(CultureInfo.InvariantCulture);
                if (v is long l) return l.ToString(CultureInfo.InvariantCulture);
                if (v is Money m) return m.Value.ToString("0.##", CultureInfo.InvariantCulture);
                return v.ToString();
            }

            string StatusPill(bool pass)
            {
                if (pass)
                    return "<span style='display:inline-block; padding:2px 10px; border-radius:999px; font-size:12px; font-weight:700; " +
                           "color:#166534; background:#dcfce7; border:1px solid #bbf7d0;'>Pass</span>";

                return "<span style='display:inline-block; padding:2px 10px; border-radius:999px; font-size:12px; font-weight:700; " +
                       "color:#991b1b; background:#fee2e2; border:1px solid #fecaca;'>Fail</span>";
            }

            EvalLine FindFirstFailedCondition(GroupEval g)
            {
                if (g == null) return null;

                var firstLocalFail = (g.conditions ?? new List<EvalLine>()).FirstOrDefault(x => x != null && !x.pass);
                if (firstLocalFail != null) return firstLocalFail;

                foreach (var child in (g.groups ?? new List<GroupEval>()))
                {
                    var f = FindFirstFailedCondition(child);
                    if (f != null) return f;
                }
                return null;
            }


            var nowUtc = DateTime.UtcNow;

            var caseText = caseRef != null ? (!string.IsNullOrWhiteSpace(caseRef.Name) ? caseRef.Name : caseRef.Id.ToString()) : "";
            var benefitText = benefitRef != null ? (!string.IsNullOrWhiteSpace(benefitRef.Name) ? benefitRef.Name : benefitRef.Id.ToString()) : "";
            var childText = recipientRef != null ? (!string.IsNullOrWhiteSpace(recipientRef.Name) ? recipientRef.Name : recipientRef.Id.ToString()) : "";

            var sb = new System.Text.StringBuilder();

            sb.Append("<div style='font-family:Segoe UI, Arial, sans-serif; font-size:13px; color:#0f172a;'>");
            sb.Append("<div style='font-size:18px; font-weight:700; margin-bottom:6px;'>Eligibility Determination</div>");

            sb.Append("<div style='margin-bottom:10px;'>");
            sb.Append($"<div><b>Date (UTC):</b> {H(nowUtc.ToString("yyyy-MM-dd HH:mm"))}</div>");
            if (!string.IsNullOrWhiteSpace(caseText)) sb.Append($"<div><b>Case:</b> {H(caseText)}</div>");
            if (!string.IsNullOrWhiteSpace(benefitText)) sb.Append($"<div><b>Service/Benefit:</b> {H(benefitText)}</div>");
            if (!string.IsNullOrWhiteSpace(childText)) sb.Append($"<div><b>Child:</b> {H(childText)}</div>");

            var color = isEligible ? "#16a34a" : "#dc2626";
            sb.Append($"<div><b>Result:</b> <span style='color:{color}; font-weight:700;'>{H(resultMessage ?? "")}</span></div>");
            sb.Append("</div>");

            // Validation failures (if any) – keep it super readable
            if (validationFailures != null && validationFailures.Count > 0)
            {
                sb.Append("<div style='margin:10px 0; padding:10px; background:#fff7ed; border:1px solid #fed7aa; border-radius:8px;'>");
                sb.Append("<div style='font-weight:700; margin-bottom:6px;'>Validation issues (fix these and run again):</div>");
                sb.Append("<ul style='margin:0; padding-left:18px;'>");
                foreach (var v in validationFailures.Where(x => !string.IsNullOrWhiteSpace(x)))
                    sb.Append($"<li>{H(v)}</li>");
                sb.Append("</ul>");
                sb.Append("</div>");
                sb.Append("</div>");
                return sb.ToString();
            }

            // Summary facts table
            if (summaryFacts != null && summaryFacts.Count > 0)
            {
                sb.Append("<div style='margin-top:10px; font-weight:700; margin-bottom:6px;'>Summary</div>");
                sb.Append("<table style='width:100%; border-collapse:collapse; border:1px solid #e5e7eb;'>");
                foreach (var sf in summaryFacts)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td style='border:1px solid #e5e7eb; padding:6px; width:45%; background:#f8fafc;'><b>{H(sf.label)}</b></td>");
                    sb.Append($"<td style='border:1px solid #e5e7eb; padding:6px;'>{H(sf.value)}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }

            
            // Criteria Summary (Top-level groups only) – exactly like your popup (Q1..Q8)
            if (groupEvals != null && groupEvals.Count > 0)
            {
                sb.Append("<div style='margin-top:14px; font-weight:700; margin-bottom:6px;'>Eligibility Criteria</div>");
                sb.Append("<table style='width:100%; border-collapse:collapse; border:1px solid #e5e7eb;'>");

                foreach (var g in groupEvals) // these are ROOT children => Q1..Q8
                {
                    var pass = g != null && g.pass;

                    // Denial reason: prefer configured denialReason, else fallback to first failed condition
                    string denial = "";
                    if (!pass)
                    {
                        denial = (g.denialReason ?? "").Trim();

                        if (string.IsNullOrWhiteSpace(denial))
                        {
                            var ff = FindFirstFailedCondition(g);
                            if (ff != null)
                            {
                                denial = $"{ff.label}: expected {F(ff.expected)} but was {F(ff.actual)}";
                            }
                            else
                            {
                                denial = "Criteria not met.";
                            }
                        }
                    }

                    sb.Append("<tr>");
                    sb.Append("<td style='border:1px solid #e5e7eb; padding:10px; background:#ffffff;'>");

                    // ✅ Only group name (no ROOT > ...)
                    sb.Append($"<div style='font-weight:600;'>{H(g.label)}</div>");

                    // ✅ Only show denial reason if FAILED
                    if (!pass)
                    {
                        sb.Append("<div style='margin-top:4px; color:#991b1b; font-size:12px;'>");
                        sb.Append($"<b>Denial reason:</b> {H(denial)}");
                        sb.Append("</div>");
                    }

                    sb.Append("</td>");

                    sb.Append("<td style='border:1px solid #e5e7eb; padding:10px; width:120px; text-align:right; background:#ffffff;'>");
                    sb.Append(StatusPill(pass));
                    sb.Append("</td>");

                    sb.Append("</tr>");
                }

                sb.Append("</table>");
            }


            sb.Append("<div style='margin-top:12px; color:#64748b; font-size:12px;'>This note was generated automatically when eligibility was checked.</div>");
            sb.Append("</div>");

            return sb.ToString();
        }


        #endregion

        #region -- Set Eligibility Helper --

        private static string GetIncomeRangeTextForCase(
    IPluginExecutionContext context,
    IOrganizationService svc,
    ITracingService tracing,
    Guid caseId)
        {
            var householdSize = (int)CountHouseHoldSize(svc, tracing, caseId);

            var bliId = GetGuidFromInput(context, IN_CaseBenefitLineItemId);
            var bli = svc.Retrieve(ENT_BenefitLineItem, bliId, new ColumnSet(FLD_BLI_Benefit));
            var serviceBenefitRef = bli.GetAttributeValue<EntityReference>(FLD_BLI_Benefit);

            if (serviceBenefitRef == null || string.IsNullOrWhiteSpace(serviceBenefitRef.Name))
                return "";

            var serviceBenefitName = serviceBenefitRef.Name.Trim();

            var eaQuery = new QueryExpression(ENT_EligibilityAdmin)
            {
                ColumnSet = new ColumnSet(FLD_EA_Name),
                TopCount = 1
            };
            eaQuery.Criteria.AddCondition(FLD_EA_Name, ConditionOperator.Equal, serviceBenefitName);

            var eligibilityAdmin = svc.RetrieveMultiple(eaQuery).Entities.FirstOrDefault();
            if (eligibilityAdmin == null) return "";

            var rangeQuery = new QueryExpression(ENT_EligibilityIncomeRange)
            {
                ColumnSet = new ColumnSet(FLD_EIR_HouseHoldSize, FLD_EIR_MinIncome, FLD_EIR_MaxIncome, ENT_SubsidyTableName),
                TopCount = 5000
            };
            rangeQuery.Criteria.AddCondition(FLD_EIR_EligibilityAdmin, ConditionOperator.Equal, eligibilityAdmin.Id);
            rangeQuery.Criteria.AddCondition(ENT_SubsidyTableName, ConditionOperator.Equal, "c");

            var ranges = svc.RetrieveMultiple(rangeQuery).Entities;

            var matched = ranges.FirstOrDefault(r =>
                r.GetAttributeValue<int?>(FLD_EIR_HouseHoldSize) == householdSize &&
                string.Equals((r.GetAttributeValue<string>(ENT_SubsidyTableName) ?? "").Trim(), "c", StringComparison.OrdinalIgnoreCase));

            if (matched == null) return "";

            var min = matched.GetAttributeValue<Money>(FLD_EIR_MinIncome)?.Value ?? 0m;
            var max = matched.GetAttributeValue<Money>(FLD_EIR_MaxIncome)?.Value ?? 0m;

            return $"{min:0.##} - {max:0.##}";
        }

        private static void UpsertEligibilityData(
    IPluginExecutionContext context,
    IOrganizationService svc,
    ITracingService tracing,
    Entity bli,
    EntityReference caseRef,
    Entity incCase,
    EntityReference benefitRef,
    EntityReference recipientRef,
    bool overallEligible,
    List<GroupEval> groupEvals,
    List<EvalLine> evalLines)
        {
            try
            {
                if (caseRef == null) return;

                var caseId = caseRef.Id;
                var bliId = bli.Id;

                // Compute deductions ONCE (reuse your function)
                var netAfter = ApplyProgramSpecificDeductionsAndUpdateCase(
                    svc, tracing, caseId, bliId,
                    out var totalDeductions,
                    out var relativeKidsCount,
                    out var medicalDeductionApplied,
                    out var selfEmployedCount
                );

                var yearlyEligibleIncome = incCase?.GetAttributeValue<Money>(FLD_CASE_YearlyEligibleIncome)?.Value ?? 0m;

                // Deduction applied text same as summary
                var appliedParts = new List<string>();
                if (relativeKidsCount > 0) appliedParts.Add($"Relative Kids (${(relativeKidsCount * 5000m):0.##})");
                if (medicalDeductionApplied) appliedParts.Add("Medical Bills ($2500)");
                if (selfEmployedCount > 0) appliedParts.Add($"Self Employment ({selfEmployedCount} x 30%)");
                var deductionAppliedText = string.Join("; ", appliedParts);

                var householdSize = (int)CountHouseHoldSize(svc, tracing, caseId);

                // Child age + disability
                int childAge = 0;
                bool childDisabled = false;

                if (recipientRef != null)
                {
                    var ben = svc.Retrieve(ENT_ContactTableName, recipientRef.Id, new ColumnSet("birthdate", "mcg_disability"));
                    var dob = ben.GetAttributeValue<DateTime?>("birthdate");
                    if (dob.HasValue) childAge = CalculateAge(dob.Value);

                    if (ben.FormattedValues != null && ben.FormattedValues.ContainsKey("mcg_disability"))
                        childDisabled = ben.FormattedValues["mcg_disability"].Equals("Yes", StringComparison.OrdinalIgnoreCase);
                    else if (ben.Attributes.Contains("mcg_disability") && ben["mcg_disability"] is bool b)
                        childDisabled = b;
                }

                // Verified checkbox from BLI verified choice
                var verifiedOs = bli.GetAttributeValue<OptionSetValue>(FLD_BLI_Verified);
                bool verified = (verifiedOs != null && verifiedOs.Value == VERIFIED_YES);

                // Self employed checkbox (any income record has self employed flag)
                var selfEmpFlag = GetAnySelfEmployedFlag(svc, tracing, caseId) ?? false;

                // Citizenship: you can store blank or reuse your doc logic (for now safe default)
                // If you want exact value, you can add a GetChildCitizenship... helper later.
                var citizenship = GetCitizenshipFromBirthCertificate(svc, tracing, recipientRef.Id);

                // Income range text
                var incomeRangeText = GetIncomeRangeTextForCase(context, svc, tracing, caseId);

                // Eligibility status text
                var statusText = overallEligible ? "Eligible" : "Not Eligible";

                // Primary denial reason
                var ineligReason = overallEligible ? "" : GetAllDenialReasonsText(groupEvals, evalLines);

                // Find existing eligibilitydata record for this BLI (update latest) else create new
                var qe = new QueryExpression(ENT_EligibilityData)
                {
                    ColumnSet = new ColumnSet("mcg_eligibilitydataid"),
                    TopCount = 1
                };
                qe.Criteria.AddCondition(FLD_ED_BLI_Lookup, ConditionOperator.Equal, bliId);
                qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                qe.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

                var existing = svc.RetrieveMultiple(qe).Entities.FirstOrDefault();

                var ed = existing != null
                    ? new Entity(ENT_EligibilityData) { Id = existing.Id }
                    : new Entity(ENT_EligibilityData);

                var county = GetCountyFromCaseAddress(svc, tracing, caseId);

                // Link to BLI (always set)
                ed[FLD_ED_BLI_Lookup] = new EntityReference(ENT_BenefitLineItem, bliId);

                // Populate mapped fields
                ed[FLD_ED_ApplicationType] = "WPA"; // per mapping (or derive from benefit if you prefer)
                ed[FLD_ED_ServiceNameBenefitName] = benefitRef?.Name ?? "";
                ed[FLD_ED_NetIncomeBeforeDeduction] = new Money(yearlyEligibleIncome);
                ed[FLD_ED_DeductionAmount] = new Money(totalDeductions);
                ed[FLD_ED_DeductionApplied] = deductionAppliedText;
                ed[FLD_ED_NetIncomeAfterDeduction] = new Money(netAfter);
                ed[FLD_ED_ChildName] = recipientRef?.Name ?? "";
                ed[FLD_ED_HouseholdSize] = householdSize;
                ed[FLD_ED_ChildAge] = childAge;
                ed[FLD_ED_ChildDisabledFlag] = childDisabled;
                ed[FLD_ED_Citizenship] = citizenship;
                ed[FLD_ED_County] = county; 
                ed[FLD_ED_CareLevel] = GetChoiceFormattedValue(bli, FLD_BLI_CareServiceLevel);
                ed[FLD_ED_CareType] = GetChoiceFormattedValue(bli, FLD_BLI_CareServiceType);
                ed[FLD_ED_BenefitFrequency] = GetChoiceFormattedValue(bli, FLD_BLI_ServiceFrequency);

                // Eligibility Amount: keep blank unless you confirm exact BLI field name
                // ed[FLD_ED_EligibilityAmount] = new Money(...);

                ed[FLD_ED_EligibilityStatus] = statusText;
                ed[FLD_ED_IncomeRange] = incomeRangeText;
                ed[FLD_ED_IneligibilityReason] = ineligReason;
                ed[FLD_ED_Verified] = verified;
                ed[FLD_ED_NumberOfRelativeKids] = relativeKidsCount;
                ed[FLD_ED_SelfEmployedSingleBothParents] = selfEmpFlag;

                ed[FLD_ED_CaseId] = caseRef.Name ?? caseRef.Id.ToString();
                ed[FLD_ED_BenefitId] = bli.GetAttributeValue<string>(FLD_BLI_BenefitId) ?? "";

                if (existing != null)
                {
                    svc.Update(ed);
                    bli["mcg_haseligibilitydata"] = true;
                    svc.Update(bli);
                    tracing.Trace($"EligibilityData UPDATED for BLI={bliId}, eligibilitydataid={existing.Id}");
                }
                else
                {
                    var newId = svc.Create(ed);
                    bli["mcg_haseligibilitydata"] = true;
                    svc.Update(bli);
                    tracing.Trace($"EligibilityData CREATED for BLI={bliId}, eligibilitydataid={newId}");
                }
            }
            catch (Exception ex)
            {
                tracing.Trace("UpsertEligibilityData FAILED (ignored): " + ex);
                // do not block eligibility evaluation
            }
        }


        private static void TrySetEligibilityStatusEligible(
    IOrganizationService service,
    ITracingService tracing,
    Guid bliId,
    Entity bliLoaded)
        {
            try
            {
                // Use already-loaded value if present
                var current = bliLoaded?.GetAttributeValue<OptionSetValue>(FLD_BLI_EligibilityStatus)?.Value;

                if (current.HasValue && current.Value == ELIG_STATUS_ELIGIBLE)
                {
                    tracing.Trace($"EligibilityStatus already Eligible for BLI {bliId}. No update needed.");
                    return;
                }

                var upd = new Entity(ENT_BenefitLineItem, bliId);
                upd[FLD_BLI_EligibilityStatus] = new OptionSetValue(ELIG_STATUS_ELIGIBLE);

                service.Update(upd);

                tracing.Trace($"Updated BLI {bliId} mcg_eligibilitystatus => Eligible ({ELIG_STATUS_ELIGIBLE}).");
            }
            catch (Exception ex)
            {
                // Don’t fail eligibility evaluation just because status update failed
                tracing.Trace("Failed to update mcg_eligibilitystatus to Eligible. " + ex);
            }
        }
        #endregion
    }
}
