﻿//////////////////////////////// 
// 
//   Copyright 2019 Battelle Energy Alliance, LLC  
// 
// 
////////////////////////////////
using System;
using System.Linq;
using System.Data;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using DataLayerCore.Model;
using CSETWeb_Api.BusinessLogic.BusinessManagers.Diagram;
using CSETWeb_Api.Models;

namespace CSETWeb_Api.BusinessManagers
{
    public class DiagramManager
    {
        /// <summary>
        /// Persists the diagram XML in the database.
        /// </summary>
        /// <param name="assessmentID"></param>
        /// <param name="diagramXML"></param>
        public void SaveDiagram(int assessmentID, string diagramXML, int lastUsedComponentNumber)
        {
            // the front end sometimes calls 'save' with an empty graph on open.  Need to 
            // prevent the javascript from doing that on open, but for now,
            // let's detect an empty graph and not save it.
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(diagramXML);
            var cellCount = xDoc.SelectNodes("//root/mxCell").Count;
            var objectCount = xDoc.SelectNodes("//root/object").Count;
            if (cellCount == 2 && objectCount == 0)
            {
                return;
            }

            using (var db = new CSET_Context())
            {
                var assessmentRecord = db.ASSESSMENTS.Where(x => x.Assessment_Id == assessmentID).FirstOrDefault();
                if (assessmentRecord != null)
                {
                    DiagramDifferenceManager differenceManager = new DiagramDifferenceManager();
                    XmlDocument oldDoc = new XmlDocument();
                    if (!String.IsNullOrWhiteSpace(assessmentRecord.Diagram_Markup))
                    {
                        oldDoc.LoadXml(assessmentRecord.Diagram_Markup);
                    }
                    differenceManager.buildDiagramDictionaries(xDoc, oldDoc);
                }
                else
                {
                    //what the?? where is our assessment
                    throw new ApplicationException("Assessment record is missing for id" + assessmentID);
                }

                assessmentRecord.LastUsedComponentNumber = lastUsedComponentNumber;
                if (!String.IsNullOrWhiteSpace(diagramXML))
                {
                    assessmentRecord.Diagram_Markup = diagramXML;
                }

                db.SaveChanges();
            }
        }


        /// <summary>
        /// Returns the diagram XML for the assessment ID.  
        /// </summary>
        /// <param name="assessmentID"></param>
        /// <returns></returns>
        public DiagramResponse GetDiagram(int assessmentID)
        {
            using (var db = new CSET_Context())
            {
                var assessmentRecord = db.ASSESSMENTS.Where(x => x.Assessment_Id == assessmentID).FirstOrDefault();

                DiagramResponse resp = new DiagramResponse();

                if (assessmentRecord != null)
                {
                    resp.DiagramXml = assessmentRecord.Diagram_Markup;
                    resp.LastUsedComponentNumber = assessmentRecord.LastUsedComponentNumber;
                    return resp;
                }

                return null;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="assessmentID"></param>
        /// <returns></returns>
        public ComponentNameMap GetComponentNamingMap()
        {
            ComponentNameMap map = new ComponentNameMap();

            using (var db = new CSET_Context())
            {
                var componentSymbols = db.COMPONENT_SYMBOLS.ToList();

                foreach (var symbol in componentSymbols)
                {
                    map.Abbreviations.Add(new ComponentName(symbol.Abbreviation, symbol.File_Name));
                }

                return map;
            }
        }
    }
}