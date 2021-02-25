#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion
using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using log4net;

namespace CTSWeb.Util
{
    // Supports a list of controls that can be applied to a DataSet and that return messages
    //      Similar to build errors or control results
    //


    public interface IControl<T>
    {
        TagList<string> Tags        { get; set; }
        int             Tolerance   { get; set; }
        
        bool Pass(T voData, MessageList roMess, Filter<string> voFilter = null);   // Returns true if no error, false otherwise
    }


    public abstract class ControlBase
    {
        public TagList<string>  Tags        { get; set; }
        public int              Tolerance   { get; set; }
    }


    //public class ControlSet : ControlBase, IControl
    //{
    //    public List<IControl> Controls = new List<IControl>();

    //    public bool Pass(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
    //    {
    //        bool bRet = true;
    //        bool bTagsRequired = (voFilter != null);
    //        bool bShouldRun = !bTagsRequired;
    //        foreach (IControl oControl in Controls)
    //        {
    //            if (bTagsRequired) bShouldRun = voFilter.Match(oControl.Tags);
    //            if (bShouldRun) bRet &= oControl.Pass(voData, roMess);
    //        }
    //        return bRet;
    //    }
    //}


    public class ControlTablesExists : ControlBase, IControl<DataSet>
    {
        public List<string> Tables;

        public bool Pass(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            bool bRet = true;
            foreach (string s in Tables)
                if (!voData.Tables.Contains(s))
                {
                    bRet = false;
                    roMess.Add("RF0210", s, voData.DataSetName);
                }
            return bRet;
        }
    }

    public class ControlColumnsExist : ControlBase, IControl<DataSet>
    {
        public string TableName;
        public List<string> RequiredColumns = new List<string>();

        public bool Pass(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            bool bRet = true;
            if (voData.Tables.Contains(TableName)) 
            {
                DataColumnCollection oCols = voData.Tables[TableName].Columns;
                foreach (string s in RequiredColumns)
                    if (!oCols.Contains(s))
                    {
                        bRet = false;
                        roMess.Add("RF0211", s, TableName);
                    }
            }
            else 
            {
                bRet = false;
                roMess.Add("RF0210", TableName, voData.DataSetName);
            }
            return bRet;
        }
    }


    public class ControlValidateColumn : ControlBase, IControl<DataSet>
    {
        public string TableName;
        public string ColName;
        public HashSet<string> AllowedValues;
        public Predicate<string> Validate;

        public ControlValidateColumn() { }

        public ControlValidateColumn(string vsTableName, string vsColName, HashSet<string> voAllowedValues)
        {
            if (vsTableName is null || vsColName is null || voAllowedValues is null) throw new ArgumentNullException();
            TableName = vsTableName;
            ColName = vsColName;
            AllowedValues = voAllowedValues;
            Validate = null;
        }

        public ControlValidateColumn(string vsTableName, string vsColName, Predicate<string> voValidate)
        {
            if (vsTableName is null || vsColName is null || voValidate is null) throw new ArgumentNullException();
            TableName = vsTableName;
            ColName = vsColName;
            AllowedValues = null;
            Validate = voValidate;
        }


        public bool Pass(DataSet voData, MessageList roMess, HashSet<int> roInvalidRows, bool vbTestInvalidRows = true, Filter<string> voFilter = null)
        {
            bool bRet = true;
            if (voData.Tables.Contains(TableName))
            {
                if (voData.Tables[TableName].Columns.Contains(ColName))
                {
                    bool bStoreInvalidRows = !(roInvalidRows is null);
                    bool bUsePredicate = !(Validate is null);
                    int iCol = voData.Tables[TableName].Columns[ColName].Ordinal;
                    Message oMessage;
                    string s;
                    int c = 0;
                    foreach (DataRow oRow in voData.Tables[TableName].Rows)
                    {
                        if (vbTestInvalidRows || (bStoreInvalidRows && !roInvalidRows.Contains(c)))
                        {
                            s = oRow[iCol].ToString();
                            if ((bUsePredicate) ? !Validate(s) : !AllowedValues.Contains(s))
                            {
                                // bRet is kept to true
                                oMessage = roMess.Add("RF0212", s, ColName);
                                oMessage.SourceRow = c + 1;
                                oMessage.SourceCol = iCol + 1;
                                if (bStoreInvalidRows) roInvalidRows.Add(c);
                            }
                        }
                        c++;
                    }
                }
                else
                {
                    bRet = false;
                    roMess.Add("RF0211", ColName, TableName);
                }
            }
            else
            {
                bRet = false;
                roMess.Add("RF0210", TableName, voData.DataSetName);
            }
            return bRet;
        }

        public bool Pass(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            return Pass(voData, roMess, null, false);
        }
    }



    public class ControlInDim : ControlBase, IControl<(Dims, string)>
    {
        private readonly Predicate<(Dims, string)> _oValidator;

        public ControlInDim(Predicate<(Dims, string)> voValidator)
        {
            _oValidator = voValidator;
        }


        public bool Pass((Dims, string) voData, MessageList roMess, Filter<string> voFilter = null)
        {
            bool bRet = true;
            if (!_oValidator(voData))
            {
                bRet = false;
                roMess.Add("RF0213", voData.Item1, voData.Item2);
            }
            return bRet;
        }
    }

}