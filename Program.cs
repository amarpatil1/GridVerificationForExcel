using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace GRID_Excel_POC
{
    class Program
    {
        static void Main(string[] args)
        {
            string exfilepath = @"D:\Project Data\GRID_Excel_POC\Excels\2017-02-24-GRID_Reviewer-Report-CCIN_emailcheck.xlsx"; //CCIN

            DataTable dt = new DataTable();
            System.Data.OleDb.OleDbConnection myConnection = null;
            System.Data.DataSet dtSet = null;
            System.Data.OleDb.OleDbDataAdapter myCommand = null;
            //If you MS Excel 2007 then use below lin instead of above line
            myConnection = new System.Data.OleDb.OleDbConnection("provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + exfilepath + "';Extended Properties=Excel 12.0;");

            myCommand = new System.Data.OleDb.OleDbDataAdapter("select * from [No completed reviews (7 yrs.)$]", myConnection);
            dtSet = new System.Data.DataSet();
            myCommand.Fill(dtSet, "[No completed reviews (7 yrs.)$]");//Sheet1$
            dt = dtSet.Tables[0];

            List<CCIN> objCcin = (from DataRow obj in dt.Rows
                                  select new CCIN
                                  {
                                      People_Unique_ID = Convert.ToString(obj.ItemArray[0]),
                                      Title = Convert.ToString(obj.ItemArray[1]),
                                      First_Name = Convert.ToString(obj.ItemArray[2]),
                                      Last_Name = Convert.ToString(obj.ItemArray[3]),
                                      Email = Convert.ToString(obj.ItemArray[4]),
                                      City = Convert.ToString(obj.ItemArray[5]),
                                      Country = Convert.ToString(obj.ItemArray[6]),
                                      Institution = Convert.ToString(obj.ItemArray[7]),
                                      INSTITUTEID = Convert.ToString(obj.ItemArray[8]),
                                      Department = Convert.ToString(obj.ItemArray[9]),
                                      ORCID = Convert.ToString(obj.ItemArray[10]),
                                      ORCIDAuthenticated = Convert.ToString(obj.ItemArray[11]),
                                      ReviewerRole = Convert.ToString(obj.ItemArray[12]),
                                      EditorRoleName = Convert.ToString(obj.ItemArray[13]),
                                      RegistrationDate = Convert.ToString(obj.ItemArray[14]),
                                      Update_Date = Convert.ToString(obj.ItemArray[15]),
                                      Date_Last_Completed = Convert.ToString(obj.ItemArray[16])
                                  }).ToList();
            LoadData(objCcin);
        }

        public static void LoadData(List<CCIN> objCcin)
        {

            dbTransferDeskService_DevEntities1 gridEntities = new dbTransferDeskService_DevEntities1();
            //fetching grid data & storing into datatable.
            var data = gridEntities.GetLinkInstitutesDataFromGrid().ToList();
            //Fetch Trusted & Non-Trusted domain names from database.
            gridEntities = new dbTransferDeskService_DevEntities1();
            StatusVarification statusVarification = new StatusVarification();
            var domainList = gridEntities.pr_GetDomainList().ToList();
            foreach (var item in objCcin)
            {
                statusVarification.grid_Yes =
                    statusVarification.truested_Yes = statusVarification.nonTruested_No = statusVarification.na = null;
                int cnt = 0;
                var emailList = item.Email.Split(';');
                foreach (string t in emailList)
                {
                    MailAddress address = new MailAddress(t.Replace(",", "").Trim());
                    string host = address.Host.ToLower().Trim();
                    ////Exact Match
                    var isVerified = data.FirstOrDefault(x => x.Newlink.ToLower().Trim().StartsWith(host, StringComparison.OrdinalIgnoreCase));
                    var status = isVerified != null ? "GRID YES" : "NA";
                    if (status == "NA")
                    {
                        ////check whether excel email ID contains anywhere in grid database.
                        isVerified = data.FirstOrDefault(x => x.Newlink.ToLower().Trim().TrimEnd('/').Contains("." + host));
                        if (isVerified != null)
                        {
                            int isNonTrusted = domainList.Count(tList => host.Contains(tList.DomainName.ToLower().Trim()) && tList.Type == "N");
                            if (isNonTrusted != 0)
                                statusVarification.nonTruested_No = "NON-TRUSTED NO";
                            else
                            {
                                statusVarification.grid_Yes = "GRID YES";
                                item.GridUrl = isVerified.link;
                                cnt = 1;
                                break;
                            }
                        }
                        else
                        {
                            ////check any grid database email ID contains for selected email ID in excel.
                            statusVarification.na = "NA";
                            foreach (var k in data)
                            {
                                host = "@" + host;
                                var kk = (host.Contains("." + k.Newlink.ToLower().Trim().TrimEnd('/')) || host.Contains("@" + k.Newlink.ToLower().Trim().TrimEnd('/')));
                                host = host.Replace("@", "").Trim();
                                if (kk == true)
                                {
                                    int isNonTrusted = domainList.Count(tList => host.Contains(tList.DomainName.ToLower().Trim()) && tList.Type == "N");
                                    if (isNonTrusted != 0)
                                        statusVarification.nonTruested_No = "NON-TRUSTED NO";
                                    else
                                    {
                                        statusVarification.grid_Yes = "GRID YES";
                                        item.GridUrl = k.link;
                                        cnt = 1;
                                        break;
                                    }
                                }

                                //else if (host.Split('.').ToArray().Length == 2 && k.Newlink.Split('.').ToArray().Length == 2)
                                //{
                                //    if (host.Split('.').ToArray()[0] == k.Newlink.Split('.').ToArray()[0])
                                //    {
                                //        statusVarification.grid_Yes = "GRID YES";
                                //        item.GridUrl = k.link;
                                //        cnt = 1;
                                //        break;
                                //    }
                                //}
                            }
                        }
                        //var input = "lrz.uni-muenchen.de";//var output = input.Substring(input.IndexOf(".") + 1).Trim(); 
                        if (host.Split('.').ToArray().Length >= 3 && host.Contains('-'))
                        {
                            var checkHypen = data.FirstOrDefault(x => x.Newlink.ToLower().Trim().Contains(host.Substring(host.IndexOf(".", StringComparison.Ordinal) + 1).Trim()));
                            if (checkHypen != null)
                            {
                                statusVarification.grid_Yes = "GRID YES";
                                item.GridUrl = checkHypen.link;
                                cnt = 1;
                                break;
                            }
                        }

                        if (cnt == 0)
                        {
                            int isTrusted = domainList.Count(tList => host.Contains(tList.DomainName.ToLower().Trim()) && tList.Type == "T");
                            if (isTrusted != 0)
                            {
                                statusVarification.truested_Yes = "TRUSTED YES";
                            }
                            else
                            {
                                //var isNonTrusted = domainList.FirstOrDefault(x => x.DomainName.StartsWith(host, StringComparison.OrdinalIgnoreCase) && x.Type == "N");
                                int isNonTrusted = domainList.Count(tList => host.Contains(tList.DomainName.ToLower().Trim()) && tList.Type == "N");
                                if (isNonTrusted != 0)
                                    statusVarification.nonTruested_No = "NON-TRUSTED NO";
                            }
                        }
                    }
                    else
                    {
                        statusVarification.grid_Yes = status;
                        item.GridUrl = isVerified.link;
                        break;
                    }
                }
                if (statusVarification.grid_Yes == null)
                {
                    if (statusVarification.truested_Yes != null)
                    {
                        item.IsVerified = statusVarification.truested_Yes;
                    }
                    else if (statusVarification.nonTruested_No != null)
                    {
                        item.IsVerified = statusVarification.nonTruested_No;
                    }
                    else
                    {
                        item.IsVerified = statusVarification.na;
                    }
                }
                else
                {
                    item.IsVerified = statusVarification.grid_Yes;
                }
            }

            ExportToExcel(objCcin);
        }


        public static void ExportToExcel(List<CCIN> objCcin)
        {
            // Load Excel application
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();

            // Create empty workbook
            excel.Workbooks.Add();

            // Create Worksheet from active sheet
            Microsoft.Office.Interop.Excel._Worksheet workSheet = excel.ActiveSheet;

            // I created Application and Worksheet objects before try/catch,
            // so that i can close them in finnaly block.
            // It's IMPORTANT to release these COM objects!!
            try
            {
                // ------------------------------------------------
                // Creation of header cells
                // ------------------------------------------------
                workSheet.Cells[1, "A"] = "People Unique ID";
                workSheet.Cells[1, "B"] = "Title";
                workSheet.Cells[1, "C"] = "First Name";
                workSheet.Cells[1, "D"] = "Last Name";
                workSheet.Cells[1, "E"] = "E-mail Address";
                workSheet.Cells[1, "F"] = "City";
                workSheet.Cells[1, "G"] = "Country";
                workSheet.Cells[1, "H"] = "Institution";
                workSheet.Cells[1, "I"] = "INSTITUTEID";
                workSheet.Cells[1, "J"] = "Department";
                workSheet.Cells[1, "K"] = "ORCID";
                workSheet.Cells[1, "L"] = "ORCID Authenticated";
                workSheet.Cells[1, "M"] = "Reviewer Role";
                workSheet.Cells[1, "N"] = "Editor Role Name";
                workSheet.Cells[1, "O"] = "Registration Date";
                workSheet.Cells[1, "P"] = "People Record Last Update Date";
                workSheet.Cells[1, "Q"] = "Date Last Completed a Review";
                workSheet.Cells[1, "R"] = "Grid Urls";
                workSheet.Cells[1, "S"] = "Status verification";
                // ------------------------------------------------
                // Populate sheet with some real data from "cars" list
                // ------------------------------------------------
                int row = 2; // start row (in row 1 are header cells)
                foreach (CCIN obj in objCcin)
                {
                    workSheet.Cells[row, "A"] = obj.People_Unique_ID; // Uncomment for CCIN List only
                    workSheet.Cells[row, "B"] = obj.Title;
                    workSheet.Cells[row, "C"] = obj.First_Name;
                    workSheet.Cells[row, "D"] = obj.Last_Name;
                    workSheet.Cells[row, "E"] = obj.Email;
                    workSheet.Cells[row, "F"] = obj.City;
                    workSheet.Cells[row, "G"] = obj.Country;
                    workSheet.Cells[row, "H"] = obj.Institution;
                    workSheet.Cells[row, "I"] = obj.INSTITUTEID;
                    workSheet.Cells[row, "J"] = obj.Department;
                    workSheet.Cells[row, "K"] = obj.ORCID;
                    workSheet.Cells[row, "L"] = obj.ORCIDAuthenticated;
                    workSheet.Cells[row, "M"] = obj.ReviewerRole;
                    workSheet.Cells[row, "N"] = obj.EditorRoleName;
                    workSheet.Cells[row, "O"] = obj.RegistrationDate;
                    workSheet.Cells[row, "P"] = obj.Update_Date;
                    workSheet.Cells[row, "Q"] = obj.Date_Last_Completed;
                    workSheet.Cells[row, "R"] = obj.GridUrl;
                    workSheet.Cells[row, "S"] = obj.IsVerified;
                    row++;
                }

                // Apply some predefined styles for data to look nicely :)
                workSheet.Range["A1"].AutoFormat(Microsoft.Office.Interop.Excel.XlRangeAutoFormat.xlRangeAutoFormatClassic1);

                // Define filename
                string fileName = string.Format(@"D:\2017-04-04-GRID_Reviewer-Report-CCIN_emailcheck.xlsx", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

                // Save this data as a file
                workSheet.SaveAs(fileName);

                // Display SUCCESS message

            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                // Quit Excel application
                excel.Quit();

                // Release COM objects (very important!)
                if (excel != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);

                if (workSheet != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workSheet);

                // Empty variables
                excel = null;
                workSheet = null;

                // Force garbage collector cleaning
                GC.Collect();
            }
        }


    }
}
