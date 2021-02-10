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


    public interface IControl
    {
        TagList<string> Tags        { get; set; }
        int             Tolerance   { get; set; }
        
        void Run(DataSet voData, MessageList roMess, Filter<string> voFilter = null);
    }


    public abstract class ControlBase
    {
        public TagList<string>  Tags        { get; set; }
        public int              Tolerance   { get; set; }
    }


    public class ControlSet : ControlBase, IControl
    {
        public List<IControl> Controls = new List<IControl>();

        public void Run(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            bool bTagsRequired = (voFilter != null);
            bool bShouldRun = !bTagsRequired;
            foreach (IControl oControl in Controls)
            {
                if (bTagsRequired) bShouldRun = voFilter.Match(oControl.Tags);
                if (bShouldRun) oControl.Run(voData, roMess);
            }
        }
    }


    public class ControlTablesExists : ControlBase, IControl
    {
        public List<string> Tables;

        public void Run(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            foreach (string s in Tables) if (!voData.Tables.Contains(s)) roMess.Add("RF0210", s, voData.DataSetName);
        }
    }

    public class ControlColumnExists : ControlBase, IControl
    {
        public string TableName;
        public List<string> RequiredColumns = new List<string>();

        public void Run(DataSet voData, MessageList roMess, Filter<string> voFilter = null)
        {
            if (voData.Tables.Contains(TableName)) 
            {
                DataColumnCollection oCols = voData.Tables[TableName].Columns;
                foreach (string s in RequiredColumns) if (!oCols.Contains(s)) roMess.Add("RF0211", s, TableName);
            }
            else 
            {
                roMess.Add("RF0210", TableName, voData.DataSetName);
            }
        }
    }
}