function callAction_GenerateDeterminationLetter() {
    // Get the current record's ID
    var recordId = Xrm.Page.data.entity.getId();

    // Get the current record's type
    var recordType = Xrm.Page.data.entity.getEntityName();

    // Get the value of the EE or Retiree field
    var eeOrRetiree = Xrm.Page.getAttribute("som_eeorretiree").getValue();

    // Get the value of the Carrier Recommendation field
    var carrierRecommendation = Xrm.Page.getAttribute("som_carrierrecommendation").getText();

    // Get the case lookup field
    var somCase = Xrm.Page.getAttribute("som_case").getValue();
    var caseId = somCase[0].id;

    // Determine the Word Template Name based on the EE or Retiree field and Carrier Recommendation field
    var wordTemplateName = "";

    if (carrierRecommendation == "approved" || carrierRecommendation == "denied") {
        if (eeOrRetiree) {
            wordTemplateName = "Incapacitated Dependent Denial (Employee)";
        } else {
            wordTemplateName = "Incapacitated Dependent Denial (Retiree)";
        }
    } else {
        wordTemplateName = "Incapacitated Dependent Review";
    }

    //Log all input parameters to the console
    console.log("recordId: " + recordId);
    console.log("recordType: " + recordType);
    console.log("eeOrRetiree: " + eeOrRetiree);
    console.log("carrierRecommendation: " + carrierRecommendation);
    console.log(somCase[0].id);

    console.log("somCase: " + somCase);
    console.log(somCase[0]);


        

    // Prepare the request
    var actionRequest = {
        WordOrPdf: "pdf",
        RecordId: recordId,
        RecordType: recordType,
        WordTemplateName: wordTemplateName,
        case: caseId
    };

        getMetadata: function () {
            return {
                boundParameter: null,
                parameterTypes: {
                    "wordorpdf": {
                        "typeName": "Edm.String",
                        "structuralProperty": 1
                    },
                    "RecordId": {
                        "typeName": "Edm.String",
                        "structuralProperty": 1
                    },
                    "RecordType": {
                        "typeName": "Edm.String",
                        "structuralProperty": 1
                    },
                    "WordTemplateName": {
                        "typeName": "Edm.String",
                        "structuralProperty": 1
                    },
                    "case": {
                        "typeName": "mscrm.incident",
                        "structuralProperty": 5
                    }
                },
                operationType: 0,
                operationName: "som_SOMWordTemplateRenderandTransactionCreation"
            };
        }
    };

    // Call the action
    Xrm.WebApi.online.execute(actionRequest).then(
        function success(result) {
            if (result.ok) {
                // Action executed successfully, you can get the response if required
                if (result.responseText) {
                    var responses = JSON.parse(result.responseText);
                }
            }

        },
        function error(error) {
            // Handle error
            console.error(error.message);
        }
    );
}
