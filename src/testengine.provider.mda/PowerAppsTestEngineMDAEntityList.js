// WARNING:
// The JavaScript object model of the page is subject to change. Do not take dependencies on the implementation of methods as they could be updated

class PowerAppsModelDrivenEntityList {
    //implementation subject to change for Client side and entity interactions
    static CONSTANTS = Object.freeze({
        MainGrid: "Items",
        MainGridRowsRecordName: "Rows",
        EntityId: "entityId"
    });

    static getMainGridControls() {
        debugger;
        //TODO: set property types other than columns and individual column properties also
        // Warning: control object population for the main grid is only for rows currently
        var propertyTypeString = "*["
        var attributes = new Set();
        var columnInfo = getCurrentXrmStatus().mainGrid.getColumnInfo();       
        attributes.add("entityId:s");
        Object.keys(columnInfo).forEach(attribute => {
            var columnName = attribute; // see how to reconcile it to displayname since it can have multiword columnInfo[attribute].DisplayName;
            var attrType = columnInfo[attribute].Type;
            //TODO: for lookup type handle case where no value is present and if value is present then set type
            var columnType = PowerAppsModelDrivenEntityList.getAttributeDataType(attrType);
            attributes.add(`${columnName}:${columnType}`);
        });
        propertyTypeString += Array.from(attributes).join(", ");
        propertyTypeString += "]";
        return JSON.stringify({ Controls: [{ Name: PowerAppsModelDrivenEntityList.CONSTANTS.MainGrid, Properties: [{ PropertyName: PowerAppsModelDrivenEntityList.CONSTANTS.MainGridRowsRecordName, PropertyType: propertyTypeString }] }] });
    }

    static fetchArrayItemCount(itemPath) {
        debugger;
        if (itemPath.controlName == PowerAppsModelDrivenEntityList.CONSTANTS.MainGrid && itemPath.propertyName == PowerAppsModelDrivenEntityList.CONSTANTS.MainGridRowsRecordName) {
            var rowCount = getCurrentXrmStatus().mainGrid.getGrid().getRows().getLength();
            return rowCount;
        }
        else {
            if (itemPath.parentControl && itemPath.parentControl.index === null) {
                // Components do not have an item count
                throw "Not a gallery, no item count available. Most likely a component";
            }
            // TODO: Identify not main grid controls of gallery type and return item count
            return 0;
        }
    }

    static getControlProperties(itemPath) {
        debugger;
        var data = [];
        // TODO: Get other Grid properties
        // getting controls for grid item 
        // TODO add recursion for control and non basic properties
        if (itemPath.parentControl && itemPath.parentControl.index !== null && itemPath.parentControl.controlName == PowerAppsModelDrivenEntityList.CONSTANTS.MainGrid) {
            // if parent is grid items and property is columns
            if (itemPath.parentControl.propertyName == PowerAppsModelDrivenEntityList.CONSTANTS.MainGridRowsRecordName) {
                if (itemPath.propertyName == "entityId") {
                    //special case for entityId since its not in attributes
                    var currentGridRow = getCurrentXrmStatus().mainGrid.getGrid().getRows().get(itemPath.parentControl.index);
                    var idVal = currentGridRow.getData().getEntity().getId();
                    idVal = idVal.replace(/{|}/g, "");
                    data.push({ Key: itemPath.propertyName, Value: idVal });
                }
                else {
                    debugger;
                  
                    // fetch the property of the current index row
                    var currentGridRow = getCurrentXrmStatus().mainGrid.getGrid().getRows().get(itemPath.parentControl.index);
                    var currentRowEntity = currentGridRow.getData().getEntity();
                    var currentAttribute = currentRowEntity.attributes.getByName(itemPath.propertyName);
                    var currentAttributeValue = currentAttribute.getValue();
                    data.push({ Key: itemPath.propertyName, Value: currentAttributeValue });
                }
            }
            else {
                //TODO non column properties
            }
        }
        else {
            // TODO non grid controls
        }
        return JSON.stringify(data);
    }

    static getAttributeDataType(attribute) {
        // value to get the notation for records based on type of attrtibute, append unknown types as required
        var attributeType;
        switch (attribute) {
            case "integer":
                attributeType = "i"; // Integer type
                break;
            case "boolean":
                attributeType = "b"; // Boolean type
                break;
            case "datetime":
                attributeType = "d"; // DateTime type
                break;
            case "decimal":
                attributeType = "n"; // Decimal type
                break;
            case "string":
            case "memo":
                attributeType = "s"; // String or memo type
                break;
            case "lookup":
                attributeType = "![id:s, name:s, entityType:s]"; // Lookup type
                break;
            case "picklist":
                attributeType = "p"; // OptionSet type
                break;
            case "money":
                attributeType = "m"; // Currency type
                break;
            // Add more cases as needed
            default:
                attributeType = "s"; // Default to 's' for string if no match
                break;
        }
        return attributeType;
        //enum AttributeType {
        //    Boolean = "boolean",
        //    Unknown = "unknown",
        //    Customer = "customer",
        //    // Date and DateTime are treated the same, Date is not a real attribute type
        //    Date = "date",
        //    DateTime = "datetime",
        //    Decimal = "decimal",
        //    Double = "double",
        //    Image = "image",
        //    Integer = "integer",
        //    Lookup = "lookup",
        //    ManagedProperty = "managedproperty",
        //    Memo = "memo",
        //    Money = "money",
        //    Owner = "owner",
        //    PartyList = "partylist",
        //    PickList = "picklist",
        //    State = "state",
        //    Status = "status",
        //    String = "string",
        //    UniqueIdentifier = "uniqueidentifier",
        //    CalendarRules = "calendarrules",
        //    Virtual = "virtual",
        //    BigInt = "bigint",
        //    EntityName = "entityname",
        //    EntityImage = "entityimage",
        //    AliasedValue = "aliasedvalue",
        //    Regarding = "regarding",
        //    MultiSelectPickList = "multiselectpicklist",
        //    File = "file",
        //    NavigationProperty = "navigationproperty",
        //    RichText = "RichText",
        //}
    }
}