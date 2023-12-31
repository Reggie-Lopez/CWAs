function splitNPADecution(spaSelectedIds) {

    var selectedId = null;
    debugger;
    for (var i = 0; i < spaSelectedIds.length; i++) {
        selectedId = selectedId != null ? selectedId + ", " + spaSelectedIds[i] : spaSelectedIds[i];
    }
    var parameters = {};
    parameters.SelectedDLs = selectedId;
  
    var req = new XMLHttpRequest();
    var globalContext = Xrm.Utility.getGlobalContext();
    req.open("POST", globalContext.getClientUrl() + "/api/data/v9.1/som_NPADeductionLineSplitProcess", false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.onreadystatechange = function () {
        if (this.readyState === 4) {
            req.onreadystatechange = null;
            if (this.status === 200 || this.status === 204) {
                setTimeout(() => {
                    Xrm.Utility.closeProgressIndicator()
                    var lookupOptions = {};
                    lookupOptions.entityType = "som_npadeductionline";
                    Xrm.Utility.refreshParentGrid(lookupOptions);
                }, 2500);
            }
            else {
                setTimeout(() => {
                    Xrm.Utility.closeProgressIndicator()
                    var lookupOptions = {};
                    lookupOptions.entityType = "som_npadeductionline";
                    Xrm.Utility.refreshParentGrid(lookupOptions);
                }, 2500);
            }
        } else {
            Xrm.Utility.alertDialog(this.statusText);
            setTimeout(() => {
                Xrm.Utility.closeProgressIndicator()
                var lookupOptions = {};
                lookupOptions.entityType = "som_npadeductionline";
                Xrm.Utility.refreshParentGrid(lookupOptions);
            }, 2500);
        }
    };
    req.send(JSON.stringify(parameters));
}