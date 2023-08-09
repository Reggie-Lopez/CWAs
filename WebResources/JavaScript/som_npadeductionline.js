if (typeof ($) === 'undefined') {
    $ = parent.$;
    jQuery = parent.jQuery;
}

function npadeductionline_OnLoad(context) {
    console.log('form onload called');
    var formContext = context.getFormContext();

    formContext.getAttribute('som_eedeductioncode').addOnChange(AddCustomFilter_ee);
    formContext.getAttribute('som_eedeductioncode').addOnChange(ClearEEAmount);
    formContext.getAttribute('som_eedeductionoptionamount').addOnChange(GetOptionAmount_ee);
    formContext.getAttribute('som_somdeductioncode').addOnChange(AddCustomFilter_som);
    formContext.getAttribute('som_somdeductioncode').addOnChange(ClearSOMAmount);
    formContext.getAttribute('som_somdeductionoptionamount').addOnChange(GetOptionAmount_som);

    AddCustomFilter_ee(context);
    AddCustomFilter_som(context);
    //formContext.getAttribute('som_eedeductioncode').addOnChange(function (e) { AddCustomFilter(type); });
}


function npadeductionline_OnSave(context) {
    console.log('form onsave called');
    var formContext = context.getFormContext();
   
}

function ClearEEAmount(executionContext) {
    var formContext = executionContext.getFormContext();  
    formContext.getAttribute("som_eedeductionamount_simple").setValue(null);
    formContext.getAttribute("som_eedeductionoptionamount").setValue(null);  
}
function ClearSOMAmount(executionContext) {
    var formContext = executionContext.getFormContext();
    formContext.getAttribute("som_somdeductionamount_simple").setValue(null);
    formContext.getAttribute("som_somdeductionoptionamount").setValue(null);
}

function AddCustomFilter_ee(executionContext) {
    var formContext = executionContext.getFormContext();
   
    //Apply Search on lookup field OnClick 
    formContext.getControl("som_eedeductionoptionamount").addPreSearch(function () {
        var eeDeductionCode = formContext.getAttribute("som_eedeductioncode").getValue();
        var dedCode = eeDeductionCode[0].id;
        dedCode = dedCode.replace('{', '').replace('}', '');
        var currentYear = new Date().getFullYear();
        //Apply the filter condition
        var filter = "<filter type='and'><condition attribute='som_deductioncode' operator='eq' uitype='som_deductionoptionamount' value='" + dedCode + "' /><condition attribute='som_year' operator='eq' value='" + currentYear + "' /></filter>";
        //Populate the filter values into lookup field
        formContext.getControl("som_eedeductionoptionamount").addCustomFilter(filter);
    });
}

function AddCustomFilter_som(executionContext) {
    var formContext = executionContext.getFormContext();
    //Apply Search on lookup field OnClick 
    formContext.getControl("som_somdeductionoptionamount").addPreSearch(function () {
        var somDeductionCode = formContext.getAttribute("som_somdeductioncode").getValue();
        var dedCode = somDeductionCode[0].id;
        dedCode = dedCode.replace('{', '').replace('}', '');
        var currentYear = new Date().getFullYear();
        //Apply the filter condition
        var filter = "<filter type='and'><condition attribute='som_deductioncode' uitype='som_deductionoptionamount' operator='eq' value='" + dedCode + "' /><condition attribute='som_year' operator='eq' value='" + currentYear + "' /></filter>";
        //Populate the filter values into lookup field
        formContext.getControl("som_somdeductionoptionamount").addCustomFilter(filter);
    });
}

function GetOptionAmount_ee(executionContext) {
    var formContext = executionContext.getFormContext();

    //get npa line fields
    var eeDeductionAmt = formContext.getAttribute("som_eedeductionoptionamount").getValue();
    if (eeDeductionAmt == null) {
        formContext.getAttribute("som_eedeductionamount_simple").setValue(null);
        return;
    }
    var dedAmt = eeDeductionAmt[0].id;
    dedAmt = dedAmt.replace('{', '').replace('}', '');

    //create query that looks for the option amount for the amount selected
    var baseQuery =
        "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
        "  <entity name='som_deductionoptionamount'>" +
        "    <attribute name='som_amount' />" +
        "    <filter type='and'><condition attribute='som_deductionoptionamountid' operator='eq' uitype='som_deductioncode' value='" + dedAmt + "' /></filter>" +
        "  </entity>" +
        "</fetch>";
    var queryForOptionAmount = "/som_deductionoptionamounts?fetchXml=" + baseQuery;
    // call function to get the record
    var _results = findFirst(queryForOptionAmount, false);   
    if (typeof (_results) !== "undefined" && _results !== null) {
        var amount = _results.som_amount;
        formContext.getAttribute("som_eedeductionamount_simple").setValue(amount);
        return;
    }
}

function GetOptionAmount_som(executionContext) {
    var formContext = executionContext.getFormContext();

    //get npa line fields
    var somDeductionAmt = formContext.getAttribute("som_somdeductionoptionamount").getValue();
    if (somDeductionAmt == null) {
        formContext.getAttribute("som_somdeductionamount_simple").setValue(null);
        return;
    }
    var dedAmt = somDeductionAmt[0].id;
    dedAmt = dedAmt.replace('{', '').replace('}', '');

    //create query that looks for the option amount for the amount selected
    var baseQuery =
        "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
        "  <entity name='som_deductionoptionamount'>" +
        "    <attribute name='som_amount' />" +
        "    <filter type='and'><condition attribute='som_deductionoptionamountid' operator='eq' uitype='som_deductioncode' value='" + dedAmt + "' /></filter>" +
        "  </entity>" +
        "</fetch>";
    var queryForOptionAmount = "/som_deductionoptionamounts?fetchXml=" + baseQuery;
    // call function to get the record
    var _results = findFirst(queryForOptionAmount, false);
    if (typeof (_results) !== "undefined" && _results !== null) {
        var amount = _results.som_amount;
        formContext.getAttribute("som_somdeductionamount_simple").setValue(amount);
        return;
    }
}


function findFirst(oDataQuery, ignoreErrors) {
    if (!oDataQuery) return null;
    oDataQuery = oDataQuery.replace(/[\r\n\t]/g, '');
    var match = null;
    $.ajax({
        type: "GET",
        async: false,
        contentType: "application/json; charset=utf-8",
        datatype: "json",
        url: getServicePath() + oDataQuery,
        beforeSend: function (XmlHttpRequest) {
            XmlHttpRequest.setRequestHeader('Accept', 'application/json');
            XmlHttpRequest.setRequestHeader('OData-Version', '4.0');
            XmlHttpRequest.setRequestHeader('OData-MaxVersion', '4.0');
            XmlHttpRequest.setRequestHeader('Prefer', 'odata.include-annotations="*"');
        },
        success: function (data, textStatus, XmlHttpRequest) {
            if (typeof (data) !== "undefined") {
                if (typeof (data.value) === "undefined") {
                    match = data;
                } else if (data.value[0] !== null) {
                    match = data.value[0];
                }
            }
        },
        error: function (XmlHttpRequest, textStatus, errorThrown) {
            if (!ignoreErrors) {
                Xrm.Navigation.openAlertDialog({ text: "OData query failed: " + oDataQuery }).then();
                Xrm.Navigation.openAlertDialog({ text: "Result: " + XmlHttpRequest.responseText + "; " + textStatus }).then();
            }
        }
    });
    if (typeof (match) === "undefined") match = null;
    return match;
}

function getServicePath() {
    var globalContext = Xrm.Utility.getGlobalContext();
    return globalContext.getClientUrl() + "/api/data/v9.2";
}
