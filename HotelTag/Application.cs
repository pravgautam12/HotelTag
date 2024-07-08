using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Collections.Generic;
using Autodesk.Revit.UI.Events;
using System.Xml.Linq;
using Autodesk.Revit.DB.Electrical;

namespace HotelTag
{
    public class Application : IExternalApplication
    {

        private static DateTime lastModificationTime = DateTime.MinValue;
        private static readonly TimeSpan modificationInterval = TimeSpan.FromSeconds(1);
        public Result OnStartup(UIControlledApplication application)
        {

            CommentUpdater commentUpdater = new CommentUpdater(application.ActiveAddInId);
            UpdaterRegistry.RegisterUpdater(commentUpdater, true);

            //Add triggers to the filter
            ElementClassFilter familyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementClassFilter electricalSystemFilter = new ElementClassFilter(typeof(ElectricalSystem));
            LogicalOrFilter combinedFilter = new LogicalOrFilter(electricalSystemFilter, familyInstanceFilter);
            UpdaterRegistry.AddTrigger(commentUpdater.GetUpdaterId(), combinedFilter, Element.GetChangeTypeElementAddition());
            UpdaterRegistry.AddTrigger(commentUpdater.GetUpdaterId(), combinedFilter, Element.GetChangeTypeAny());


            String tabName = "HotelTag";
            application.CreateRibbonTab(tabName);

            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "HotelTag");
            ribbonPanel.Name = "HotelTag";

            PushButtonData b1Data = new PushButtonData("Add tag", "Add tag", Assembly.GetExecutingAssembly().Location, "HotelTag.jBoxTag");
            ribbonPanel.AddItem(b1Data);


            //application.Idling += OnIdling;

            return Result.Succeeded;
        }



        public Result OnShutdown(UIControlledApplication application)
        {
            // Unregister the updater
            UpdaterRegistry.UnregisterUpdater(new CommentUpdater(application.ActiveAddInId).GetUpdaterId());

            application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
            return Result.Succeeded;
        }

        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {

            TaskDialog.Show("YOU TRIGGERED?", "Don't worry! It's just IUpdater");

        }


    }

    public class CommentUpdater : IUpdater
    {
        public static UpdaterId _updaterId;
        public static int _caseSwitch = 1;
        



        public CommentUpdater(AddInId id)
        {
            _updaterId = new UpdaterId(id, new Guid("77A928B1-3AEF-4602-AAE5-2C4054437411"));
        }

        public void Execute(UpdaterData data)
        {
            bool elementProcessed = false;

            switch (_caseSwitch)
            {

                case 1:

                    Document doc = data.GetDocument();

                    //UpdaterRegistry.DisableUpdater(_updaterId);
                    ICollection<ElementId> modifiedElements = data.GetModifiedElementIds();

                    if (modifiedElements.Count > 0)
                    {
                        foreach (ElementId elementId in modifiedElements)
                        {
                            Element element = doc.GetElement(elementId);

                            //if (element != null && element.Name == "xx")

                            if (element is FamilyInstance) {
                                if (element != null && (element as FamilyInstance).Symbol.Family.Name == "se_Hotel Unit")
                                {
                                    string updatingTag = "";
                                    int n = 0;
                                    jBoxTag.EditCommentsOnly(element as FamilyInstance, updatingTag, n, doc);
                                    elementProcessed = true;
                                }
                            }

                            else
                            {
                                try
                                {
                                    ElectricalSystem elecSys = element as ElectricalSystem;
                                    ElementSet elementSet = elecSys.Elements;
                                    foreach (Element elem in elementSet)
                                    {
                                        FamilyInstance famins = elem as FamilyInstance;
                                        if (famins != null && famins.Symbol.Family.Name == "se_Hotel Unit")
                                        {
                                            string updatingTag = "";
                                            int n = 0;
                                            jBoxTag.EditCommentsOnly(famins, updatingTag, n, doc);
                                            elementProcessed = true;
                                        }
                                    }


                                }
                                catch
                                {

                                }
                            }
                        }


                        //_caseSwitch = 2;
                    }

                    //else
                    //{
                    //    _caseSwitch = 1;
                    //}
                    //break;

                    if (elementProcessed)
                    {
                        _caseSwitch = 2;
                    }
                    break;

                case 2:
                    _caseSwitch = 1;
                    break;

            }

            //UpdaterRegistry.EnableUpdater(_updaterId);

        }

        public string GetAdditionalInformation()
        {
            return "Updates";
        }
        public ChangePriority GetChangePriority()
        {
            return ChangePriority.MEPFixtures;
        }

        public UpdaterId GetUpdaterId()
        {
            return _updaterId;
        }

        public String GetUpdaterName()
        {
            return "suppy puppy";
        }


    }
}

