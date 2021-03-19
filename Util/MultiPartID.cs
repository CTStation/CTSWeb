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

		public bool TryGet(string vsName, out tObject roNamed) => _oIndex.TryGetValue(vsName, out roNamed);

		// ICollection interface
		public void Add(tObject voNamed) { _oIndex.Add(voNamed.Name, voNamed); }
		public void Clear() { _oIndex.Clear(); }
		public bool Contains(tObject voNamed) => _oIndex.ContainsKey(voNamed.Name);
		public void CopyTo(tObject[] raoNodes, int viIndex) { throw new NotImplementedException(); }
		public int Count { get => _oIndex.Count; }
		public bool IsReadOnly { get => false; }
		public bool Remove(tObject voNamed) => _oIndex.Remove(voNamed.Name);
		IEnumerator IEnumerable.GetEnumerator() => _oIndex.Values.GetEnumerator();
		public IEnumerator<tObject> GetEnumerator() => _oIndex.Values.GetEnumerator();
		public object SyncRoot { get { throw new NotImplementedException(); } }
		public bool IsSynchronized { get { throw new NotImplementedException(); } }
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
		public readonly List<NodeDesc> Nodes = new List<NodeDesc>();

		public MultiPartID(
			  Context roContext
			, Func<ICtObject, Context, List<ManagedObject>> roGetIdentifierParts
			, Func<Context, List<string>> roDimDescList
			, Predicate<ICtObject> roFilter = null
		)
		{
			List<ManagedObject> oID;
			List<NamedObjectCollection<NodeDesc>> oIndexes = new List<NamedObjectCollection<NodeDesc>>();
			NodeDesc oUpperNode;
			NodeDesc oNode;
			int c;
			Manager.Execute<tObject>(roContext, (ICtObjectManager roMgr) => {
				ICtGenCollection oColl = roMgr.GetObjects(null, ACCESSFLAGS.OM_READ, 0, null);
				foreach (ICtObject o in oColl)
				{
					if ((roFilter is null) || roFilter(o))
					{
						oID = roGetIdentifierParts(o, roContext);
						// Create indexes and dim markers on first run
						if (oIndexes.Count == 0) foreach (ManagedObject oPart in oID)  oIndexes.Add(new NamedObjectCollection<NodeDesc>()); 
						Debug.Assert(oIndexes.Count == oID.Count);
						c = 0;
						oUpperNode = null;
						foreach (ManagedObject oPart in oID)
						{
							if (!oIndexes[c].TryGet(oPart.Name, out oNode))
							{
								oNode = new NodeDesc(oPart);
								oIndexes[c].Add(oNode);
							}
							Debug.Assert(oIndexes[c].Contains(oNode) && oNode.Name == oPart.Name && oPart.Name == oID[c].Name);
							if (!(oUpperNode is null)) oUpperNode.Add(oNode);
							oUpperNode = oNode;
							c++;
						}
					}
				}
			});
			// Here oIndexes[0] is a dictionary representing the table. Transform it into a collection for better serialization
			if (0 < oIndexes.Count)
            {
				foreach (var o in oIndexes[0])
					Nodes.Add(o);
				Dims = roDimDescList(roContext);
			}
            else
            {
				Dims = new List<string>();
            }
		}
	}
}
