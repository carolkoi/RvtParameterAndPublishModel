using Newtonsoft.Json;
using System.Net.Http;


namespace PublishModelDocs.Services
{
    public class PublishModel
    {
        //step #0. You can check if you have permissions to upoad to this folder first 
        //https://aps.autodesk.com/en/docs/data/v2/reference/http/CheckPermission/
        // This will try avoid 403 errors if the user does not have permissions. Checked this manually via postman
        // TBD

        //step #1. Verifies whether a Collaboration for Revit (C4R) model needs to be published to BIM 360 Docs.(I checked this manually through postman,
        //You can implement just to verify. I will implement later 
        //https://aps.autodesk.com/en/docs/data/v2/reference/http/GetPublishModelJob/
        //TBD


        // step #2. Data management API POST to publish model
        //https://aps.autodesk.com/en/docs/data/v2/reference/http/PublishModel/
        //Done

        //step #3. Verify the model has finished publishing
        //https://aps.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-items-item_id-GET/
        //TBD

        public async Task<bool> PublishRvtModelInDocs(string token, string projectId, string itemId)
        {
            //var accId = accountId.Replace("b.", "");
            //var gpId = accountId.Replace("b.", "");
            //var colId = collectionId.Replace("b.", "");
            //var projectId = "b.c91c371f-1943-43a0-83b0-b14c142ded59";
            //var itemId = "urn:adsk.wipprod:dm.lineage:kY2bzTFrTL6lHr4EZKHfXA";
            //prepare payload as :{
            //        "jsonapi": {
            //            "version": "1.0"
            //        },
            //       "data": {
            //            "type": "commands",
            //            "attributes": {
            //                "extension": {
            //                    "type": "commands:autodesk.bim360:C4RModelPublish",
            //                    "version": "1.0.0"
            //                             }
            //                 },
            //            "relationships": {
            //                "resources": {
            //                    "data": [
            //                        {
            //                        "type": "items",
            //                        "id": "urn:adsk.wipprod:dm.lineage:2jxp6C-PTFqGIlE891PuOg"
            //                        }
            //            ]
            //        }
            //            }
            //        }
            //    }
            var dataParams = new[]
            {
                new
                {
                    type="items",
                    id = itemId
                }

            };

            var resourcesParam = new
            {
                data = dataParams
            };
            var relationshipsParam = new
            {
                resources = resourcesParam
            };

            var extensionParam = new
            {
                type = "commands:autodesk.bim360:C4RModelPublish",
                version = "1.0.0"
            };
            var attributesParam = new
            {
                extension = extensionParam
            };

            var dataMainParam = new
            {
                type = "commands",
                attributes = attributesParam,
                relationships = relationshipsParam

            };
            var jsonParam = new
            {
                version = "1.0"
            };

            var payload = new
            {
                jsonapi = jsonParam,
                data = dataMainParam,

            };
           

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://developer.api.autodesk.com/data/v1/projects/" + projectId + "/commands");

            request.Headers.Add("Authorization", "Bearer " + token);

            string jsonStr = JsonConvert.SerializeObject(payload);

            Console.WriteLine(" Json Serialized payload ", jsonStr);
            var content = new StringContent(jsonStr, null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            Console.WriteLine("ublish response", response);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
