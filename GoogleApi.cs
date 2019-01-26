using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.GData;
using Google.Spreadsheets;

namespace Slothu
{
    class GoogleApi
    {
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string AppName = "SlothuDiscord";
        private SheetsService Service;
        private UserCredential Credential;
        private string CredPath;
        private string PublicSheets = "16Yp-eLHQtgY05q6WBYA2MDyvQPmZ4Yr3RHYiBCBj2Hc";
        private string CellRange = "Dark!A16:G17";
        private string AuthorCell = "Dark!D18";

        public GoogleApi()
        {
            using(FileStream stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                CredPath = "token.json";
                Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(CredPath, true)).Result;
                Console.WriteLine("DEBUG: Credential file saved to: " + CredPath);
            }

            Service = new SheetsService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = AppName,
                }
            );
        }

        public IList<IList<Object>> GetPortables()
        {
            SpreadsheetsResource.ValuesResource.GetRequest locRequest =
                Service.Spreadsheets.Values.Get(PublicSheets, CellRange);;
            locRequest.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.COLUMNS;

            ValueRange locResponse = locRequest.Execute();
            IList<IList<Object>> locValues = locResponse.Values;

            return locValues;
        }

        public Object GetAuthor()
        {
            SpreadsheetsResource.ValuesResource.GetRequest authorRequest =
                Service.Spreadsheets.Values.Get(PublicSheets, AuthorCell);

            ValueRange authorResponse = authorRequest.Execute();
            Object authorValue = authorResponse.Values[0][0];
            return authorValue;
        }

    }
}
