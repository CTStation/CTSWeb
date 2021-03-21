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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using CTCLIENTSERVERLib;

namespace CTSWeb.Util
{
	// Builds a data structure that helps a user pick one row in a table whose primary key consists of multiple string columns
	// Rather than a table	A	Actual	V1	Production version
	//						A	Actual	T	Test
	// it returns			{A Actual {V1 prod, T Test}}



	// -----------------------------------------
	//		Single part identity
	// -----------------------------------------

	// First represent a data structure corresponding to an SQL table with a single string column primary key

	// An object identified by a code, representing one row in the table
	public interface INamedObject
	{
		string Name { get; set; }
	}


	// A table of objects with Name as primary key. Throws exception if duplicates
	public class NamedObjectCollection<tObject> : ICollection<tObject> where tObject : INamedObject
	{
		private Dictionary<string, tObject> _oIndex = new Dictionary<string, tObject>();

		public bool TryGet(string vsName, out tObject roNamed)		=> _oIndex.TryGetValue(vsName, out roNamed);
		public void AddIfNew(tObject voNamed)						{ if (!_oIndex.ContainsKey(voNamed.Name)) _oIndex.Add(voNamed.Name, voNamed); }

		// ICollection interface
		public void Add(tObject voNamed)							{ _oIndex.Add(voNamed.Name, voNamed); }
		public void Clear()											{ _oIndex.Clear(); }
		public bool Contains(tObject voNamed)						=> _oIndex.ContainsKey(voNamed.Name);
		public void CopyTo(tObject[] raoNodes, int viIndex)			{ throw new NotImplementedException(); }
		public int Count											{ get => _oIndex.Count; }
		public bool IsReadOnly										{ get => false; }
		public bool Remove(tObject voNamed)							=> _oIndex.Remove(voNamed.Name);
		IEnumerator IEnumerable.GetEnumerator()						=> _oIndex.Values.GetEnumerator();
		public IEnumerator<tObject> GetEnumerator()					=> _oIndex.Values.GetEnumerator();
		public object SyncRoot										{ get { throw new NotImplementedException(); } }
		public bool IsSynchronized									{ get { throw new NotImplementedException(); } }
	}



	// -----------------------------------------
	//		Multi part identity
	// -----------------------------------------

	// A node in our user-interface-optimized data structure that represents a table where the primary key is composed of multiple string columns
	// The node stores the info of one level and the list of the next level nodes
	// Another, less general node
	public class NodeDesc : INamedObject
	{
		public string Name { get; set; }
		public string Desc;
		public List<NodeDesc> Next;

		public NodeDesc(ManagedObject o)
        {
			Name = (o.Name is null) ? "" : o.Name;
			Desc = (o.LDesc is null) ? "" : o.LDesc;
        }

		public void Add(NodeDesc o) { if (Next is null) Next = new List<NodeDesc>(); Next.Add(o); }
	}


	// A class to build our structure from an FC manager
	public class MultiPartID<tObject> where tObject : ManagedObject, new()

	{
		public readonly List<string> Dims;
		public readonly List<NodeDesc> Nodes;

		public MultiPartID() { }

		// Build from manager
		public MultiPartID(
			  Context roContext
			, Func<ICtObject, Context, List<ManagedObject>> roGetIdentifierParts
			, Func<Context, List<string>> roDimDescList
			, Predicate<ICtObject> roFilter = null
		)
		{
			List<ManagedObject> oID;
			List<NamedObjectCollection<NodeDesc>> oIndexes = new List<NamedObjectCollection<NodeDesc>>();
			Manager.Execute<tObject>(roContext, (ICtObjectManager roMgr) => {
				ICtGenCollection oColl = roMgr.GetObjects(null, ACCESSFLAGS.OM_READ, 0, null);
				foreach (ICtObject o in oColl)
				{
					if ((roFilter is null) || roFilter(o))
					{
						oID = roGetIdentifierParts(o, roContext);
						PrAddID(oID, oIndexes);
					}
				}
			});
			PrBuildMultipart(roDimDescList(roContext), oIndexes[0], out Dims, out Nodes);
		}


		// Build from list of ids
		public MultiPartID(List<string> voDimensionNames, List<List<ManagedObject>>	voTable)
		{
			List<NamedObjectCollection<NodeDesc>> oIndexes = new List<NamedObjectCollection<NodeDesc>>();
			foreach (List<ManagedObject> oID in voTable)
			{
				PrAddID(oID, oIndexes);
			}
			PrBuildMultipart(voDimensionNames, oIndexes[0], out Dims, out Nodes);
		}


		private void PrAddID(List<ManagedObject> voID, List<NamedObjectCollection<NodeDesc>> roIndexes)
        {
			NodeDesc oUpperNode;
			NodeDesc oNode;
			int c;
			// Create indexes and dim markers on first run
			if (roIndexes.Count == 0) foreach (ManagedObject oPart in voID) roIndexes.Add(new NamedObjectCollection<NodeDesc>());
			Debug.Assert(roIndexes.Count == voID.Count);
			c = 0;
			oUpperNode = null;
			foreach (ManagedObject oPart in voID)
			{
				if (!roIndexes[c].TryGet(oPart.Name, out oNode))
				{
					oNode = new NodeDesc(oPart);
					roIndexes[c].Add(oNode);
					if (!(oUpperNode is null))
                    {
						Debug.Assert(0 < c);
						oUpperNode.Add(oNode);
                    }
				}
				Debug.Assert(roIndexes[c].Contains(oNode) && oNode.Name == oPart.Name && oPart.Name == voID[c].Name);
				if (c < voID.Count -1)
				{
					roIndexes[c + 1].Clear();
					if (!(oNode.Next is null)) foreach (NodeDesc oCur in oNode.Next) roIndexes[c + 1].AddIfNew(oCur);
				}
				oUpperNode = oNode;
				c++;
			}
		}


		private void PrBuildMultipart(List<string> voDimensionNames, NamedObjectCollection<NodeDesc> voIndex, out List<string> roDims, out List<NodeDesc> roNodes)
        {
			// Here oIndexes[0] is a dictionary representing the table. Transform it into a collection for better serialization
			roDims = new List<string>();
			roNodes = new List<NodeDesc>();
			if (0 < voIndex.Count)
			{
				roDims = voDimensionNames;
				foreach (var o in voIndex)	roNodes.Add(o);
			}
		}

	}
}
