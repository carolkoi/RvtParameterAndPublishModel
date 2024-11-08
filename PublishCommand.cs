using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PublishModelDocs.Services;
using RevitParametersAddin.TokenHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitParametersAddin
{
    [Transaction(TransactionMode.Manual)]
    internal class PublishCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            //Get application and document objects  
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Lets modify the model a little to sync with central cloud model later

            //I have used the Revit 2025 and my sample model which I will share with you (you can use any model of choice)

            //Define a reference Object to accept the pick result  
            Reference pickedref;

            //Pick a group  
            Selection sel = uiapp.ActiveUIDocument.Selection;
            pickedref = sel.PickObject(ObjectType.Element, "Please select a group");

            Element elem = doc.GetElement(pickedref);
            Group? group = elem as Group;

            //Pick point  
            XYZ point = sel.PickPoint("Please pick a point to place group");

            //Place the group  
            Transaction trans = new Transaction(doc);
            trans.Start("Lab");
            doc.Create.PlaceGroup(point, group?.GroupType);
            trans.Commit();

            // The whole of this is a simple sample provided on My First Revit Plugin 
            // https://www.autodesk.com/support/technical/article/caas/tsarticles/ts/7I2bC1zUr4VjJ3U31uM66K.html
            //Basically a sample to move objects from one room to another.

            // Here we will check if the model is save in cloud, if not it will through an exception later when syncing with central. I manually initiated collaboration in 
            //Revit and saved in cloud. you can as well use SaveAsCloudModel  Revit method.
            if (doc.IsModelInCloud)
            {
                Console.WriteLine("Model is in cloud");

                //Get the ACC params related to the active model doc from cloud
                var hubId = doc.GetHubId();
                var projectId = doc.GetProjectId();
                var folderId = doc.GetCloudFolderId(true);
                var modelUrn = doc.GetCloudModelUrn();


                Console.WriteLine("Hub ID:{}, projectID :{}, Folder ID: {}, Doc cloud URN: {} ", doc.GetHubId(), doc.GetProjectId(), doc.GetCloudFolderId(true), doc.GetCloudModelUrn());

                //Sync your changes with central
                // Set options for accessing central model
                TransactWithCentralOptions transactOptions = new TransactWithCentralOptions();
                // Set options for synchronizing with central
                SynchronizeWithCentralOptions syncOptions = new SynchronizeWithCentralOptions();
                // Sync with relinquishing any checked out elements or worksets  (ensure to relinquish checked out elements or worksets)
                RelinquishOptions relinquishOpts = new RelinquishOptions(true);
                syncOptions.SetRelinquishOptions(relinquishOpts);
                syncOptions.Comment = "publishing C4R model";


                try
                {
                    //doc.SaveCloudModel();
                    doc.SynchronizeWithCentral(transactOptions, syncOptions);
                }
                catch (Exception e)
                {
                    TaskDialog.Show("Synchronize Failed", e.Message);
                }

                //lets get the token to be able to call the Data Management APIs
                var token = TokenHandler.Login();
                string _token = token.ToString();
                Console.WriteLine("Token generated successfully", _token);

                // The service handling tha DA APIs
                PublishModel publishModel = new PublishModel();

                publishModel.PublishRvtModelInDocs(_token, projectId,modelUrn);

            }
            else
            {
                Console.WriteLine("Model is NOT saved in cloud or initiated for collaboration");
            }

         
            return Result.Succeeded;
        }
    }
}
