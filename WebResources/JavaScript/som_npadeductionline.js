function npadeductionline_OnLoad(context) {
    console.log('form onload called');

    formContext.getAttribute('som_eedeductioncode').addOnChange(AddCustomFilter_ee);
    formContext.getAttribute('som_somdeductioncode').addOnChange(AddCustomFilter_som);

    //formContext.getAttribute('som_eedeductioncode').addOnChange(function (e) { AddCustomFilter(type); });
}


function npadeductionline_OnSave(context) {
    console.log('form onsave called');
    var formContext = context.getFormContext();
   
}

function AddCustomFilter_ee(executionContext) {
    var formContext = executionContext.getFormContext();
    //Check if the field is not null
    var eeDeductionCode = formContext.getAttribute("som_eedeductioncode").getValue();
    if (eeDeductionCode != null) {
        //Apply Search on lookup field OnClick 
        formContext.getControl("som_eedeductionoptionamount").addPreSearch(function () {
            var dedCode = eeDeductionCode[0].id;
            dedCode = dedCode.replace('{', '').replace('}', '');
            var currentYear = new Date().getFullYear();
            //Apply the filter condition
            var filter = "<filter type='and'><condition attribute='som_deductioncode' operator='eq' uitype='som_deductionoptionamount' value='" + dedCode + "' /><condition attribute='som_year' operator='eq' value='" + currentYear + "' /></filter>";
            //Populate the filter values into lookup field
            formContext.getControl("som_eedeductionoptionamount").addCustomFilter(filter);
        });
    }
}

function AddCustomFilter_som(executionContext) {
    var formContext = executionContext.getFormContext();
    //Check if the field is not null
    var somDeductionCode = formContext.getAttribute("som_somdeductioncode").getValue();
    if (somDeductionCode != null) {
        //Apply Search on lookup field OnClick 
        formContext.getControl("som_somdeductionoptionamount").addPreSearch(function () {
            var dedCode = somDeductionCode[0].id;
            dedCode = dedCode.replace('{', '').replace('}', '');
            var currentYear = new Date().getFullYear();
            //Apply the filter condition
            var filter = "<filter type='and'><condition attribute='som_deductioncode' uitype='som_deductionoptionamount' operator='eq' value='" + dedCode + "' /><condition attribute='som_year' operator='eq' value='" + currentYear + "' /></filter>";
            //Populate the filter values into lookup field
            formContext.getControl("som_somdeductionoptionamount").addCustomFilter(filter);
        });
    }
}