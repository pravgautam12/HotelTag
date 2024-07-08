using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HotelTag
{

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class jBoxTag : IExternalCommand
    {
        string jBoxName = "xx";
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet Elements)
        {
            UIApplication uiapp = cmdData.Application as UIApplication;
            Document doc = uiapp.ActiveUIDocument.Document;
    
            IList<ViewPlan> views = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().Cast<ViewPlan>().
            Where(v => v.IsTemplate == false && v.ViewType == ViewType.FloorPlan && v.Name.Contains("POWER")).ToList();
            //List<FamilyInstance> hotelJBoxes = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalFixtures).WhereElementIsNotElementType()
            //.OfType<FamilyInstance>().Where(e => e.Symbol.Family.Name == "se_Hotel Unit" && e.Symbol.Name == jBoxName).ToList<FamilyInstance>();

            List<FamilyInstance> hotelJBoxes = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalFixtures).WhereElementIsNotElementType()
            .OfType<FamilyInstance>().Where(e => e.Symbol.Family.Name == "se_Hotel Unit").ToList<FamilyInstance>();
            IList<Element> tags = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalFixtureTags).WhereElementIsElementType().Where(e => e.Name == "Guestroom Feeder (3 Ckts)" || e.Name == "Guestroom Feeder (7 Ckts)").ToList();

            Transaction trans = new Transaction(doc, "editing comments and adding tag");
            trans.Start();

            int n = 0;
            string updatingTag = "";
            foreach (ViewPlan v in views)
            {
                bool x = EditComments(v, updatingTag, doc, n);
            }

            trans.Commit();

            return Result.Succeeded;
        }

       
        public bool EditComments(ViewPlan v, string updatingTag, Document doc, int n)
        {
            List<FamilyInstance> hotelJBoxes = new FilteredElementCollector(doc, v.Id).OfCategory(BuiltInCategory.OST_ElectricalFixtures).WhereElementIsNotElementType()
            .OfType<FamilyInstance>().Where(e => e.Symbol.Family.Name == "se_Hotel Unit" && e.Symbol.Name == jBoxName).ToList<FamilyInstance>();
            
            IList<Element> tags = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ElectricalFixtureTags).WhereElementIsElementType().Where(e => e.Name == "Guestroom Feeder (3 Ckts)" || e.Name == "Guestroom Feeder (7 Ckts)").ToList();

            
            if (hotelJBoxes.Count > 0)

            {
                foreach (FamilyInstance hotelJBox in hotelJBoxes)
                {
                    int a = EditCommentsOnly(hotelJBox, updatingTag, n, doc);
                    //if (n <= 3 && a != true)
                    if (a > 0 && a <= 3)
                    {
                        Element tagType = tags.FirstOrDefault(e => e.Name == "Guestroom Feeder (3 Ckts)") as Element;
                        IndependentTag.Create(doc, tagType.Id, v.Id, new Reference(hotelJBox), false, TagOrientation.Horizontal, (hotelJBox.Location as LocationPoint).Point);
                        
                    }
                    //if ( 3 < n && n <= 7 && a != true)
                    if (a > 3)
                    {
                        Element tagType = tags.FirstOrDefault(e => e.Name == "Guestroom Feeder (7 Ckts)") as Element;
                        IndependentTag.Create(doc, tagType.Id, v.Id, new Reference(hotelJBox), false, TagOrientation.Horizontal, (hotelJBox.Location as LocationPoint).Point);
                        
                    }
                }

                if (n > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static int EditCommentsOnly(FamilyInstance hotelJBox, string updatingTag, int n, Document doc)
        {
            MEPModel jBoxMEPModel = hotelJBox.MEPModel;
            ConnectorManager c = jBoxMEPModel.ConnectorManager;
            ConnectorSet connectors = c.Connectors;
            var sortedConnectors = connectors.Cast<Connector>().Select(connector => new
            {
                Connector = connector,
                Order = int.TryParse(connector.Description.Split(')')[0].Trim(), out int order) ? order : int.MaxValue
            }).OrderBy(x => x.Order).Select(x => x.Connector).ToList();
            updatingTag = "";
            foreach (Connector connector in sortedConnectors)
            {
                ConnectorSet connectorReferences = connector.AllRefs;
                if (connectorReferences != null)
                {
                    ElectricalSystem firstReferenceElecSys = null;
                    foreach (Connector reference in connectorReferences)
                    {
                        firstReferenceElecSys = reference.Owner as ElectricalSystem;
                        break;
                    }
                    if (firstReferenceElecSys != null)
                    {
                        string circuitNumber = firstReferenceElecSys.PanelName + " - " + firstReferenceElecSys.CircuitNumber;


                        if (connector == sortedConnectors[0])
                        {
                            updatingTag += circuitNumber;
                        }
                        else
                        {
                            updatingTag = updatingTag + " " + circuitNumber;
                        }

                        
                        hotelJBox.LookupParameter("SE_E_COMMENTS").Set(updatingTag);
                        n++;
                    }
                }
            }
            
            FilteredElementCollector tagCollector = new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)).OfCategory(BuiltInCategory.OST_ElectricalFixtureTags);
            foreach (IndependentTag tag in tagCollector)
            {
                ICollection<LinkElementId> taggedElementIds = tag.GetTaggedElementIds();
                foreach (LinkElementId linkElementId in taggedElementIds)
                {
                    if (linkElementId.HostElementId == hotelJBox.Id)
                    {
                        return 0;
                    }
                }
            }
            return n;
        }
    }
}