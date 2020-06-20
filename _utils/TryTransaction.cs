using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamoCZ.Utils
{
    [IsVisibleInDynamoLibrary(false)]
    public static class Transactions
    {
        public static bool TryTransaction(Document doc, Action a, string name = "Command")
        {
            try
            {
                using (Transaction tr = new Transaction(doc, name))
                {
                    tr.Start();

                    try
                    {
                        a.Invoke();
                    }
                    catch
                    {
                        tr.RollBack();
                        throw;
                    }
                    tr.Commit();
                }
                return true;
            }
            catch
            {
                try
                {

                    using (SubTransaction tr = new SubTransaction(doc))
                    {
                        tr.Start();
                        try
                        {
                            a.Invoke();
                        }
                        catch
                        {
                            tr.RollBack();
                            throw;
                        }
                        tr.Commit();
                    }
                    return true;
                }
                catch
                {
                    try
                    {
                        a.Invoke();
                        return true;
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }

        public static bool TryRollbackTransaction(Document doc, Action a)
        {
            try
            {
                using (Transaction tr = new Transaction(doc, "Command"))
                {
                    tr.Start();

                    try
                    {
                        a.Invoke();
                    }
                    catch
                    {
                        tr.RollBack();
                        throw;
                    }
                    tr.RollBack();
                }
                return true;
            }
            catch
            {
                try
                {

                    using (SubTransaction tr = new SubTransaction(doc))
                    {
                        tr.Start();
                        try
                        {
                            a.Invoke();
                        }
                        catch
                        {
                            tr.RollBack();
                            throw;
                        }
                        tr.RollBack();
                    }
                    return true;
                }
                catch
                {
                    try
                    {
                        a.Invoke();
                        return true;
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }
    }
}
