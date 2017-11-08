using AcETransmit;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;


[assembly: CommandClass(typeof(ArxApp.Commands))]
[assembly: ExtensionApplication(null)]

namespace ArxApp
{
    public class Parameters
    {
        public bool ExtractBlockNames { get; set; }
        public bool ExtractLayerNames { get; set; }
        public bool ExtractDependents { get; set; }
    }

    public class GeneralHelper : IDisposable
    {
        private Database db;
        private Transaction tr;

        public GeneralHelper()
        {
            db = Application.DocumentManager.MdiActiveDocument.Database;
            tr = db.TransactionManager.StartTransaction();
        }

        public GeneralHelper(Database db, Transaction tr)
        {
            this.db = db;
            this.tr = tr;
        }
        public void Dispose()
        {
            if (tr != null && !tr.IsDisposed)
            {
                tr.Dispose();
            }
        }
        public IEnumerable<LayerTableRecord> GetLayers()
        {
            var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in table)
            {
                yield return (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
            }
        }

        public IEnumerable<BlockTableRecord> GetBlocks()
        {
            var table = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId id in table)
            {
                yield return (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            }
        }
    }

    public class Commands
    {
        static void DisplayDependent(TransmittalFile tf, JsonOutput output)
        {
            int numberOfDependents = tf.numberOfDependents;
            for (int i = 0; i < numberOfDependents; ++i)
            {
                TransmittalFile childTF = tf.getDependent(i);
                FileType ft = childTF.FileType;
                var jsondependent = new Dependent();
                jsondependent.Name = ft.ToString();
                jsondependent.Path = childTF.sourcePath;
                output.Dependents.Add(jsondependent);
                DisplayDependent(childTF, output);
            }
        }

        public class Block
        {
            public string Name { set; get; }
            public string Comments { set; get; }
            public List<AttributeDef> Attributes { set; get; }
        }

        public class AttributeDef
        {
            public string Tag { set; get; }
            public string Text { set; get; }
        }

        public class Dependent
        {
            public string Name { set; get; }
            public string Path { set; get; }
        }

        public class Layer
        {
            public string Name { set; get; }
            public string Description { set; get; }
        }

        public class JsonOutput
        {
            public List<Block> Blocks { set; get; }
            public List<Dependent> Dependents { set; get; }
            public List<Layer> Layers { set; get; }
        }

        [CommandMethod("QueryDWGCommands", "querydwg", CommandFlags.Modal)]
        static public void QueryDwg()
        {
            //prompt for input json and output folder
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var res1 = ed.GetFileNameForOpen("Specify parameter file");
            if (res1.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                return;
            /*var res2 = ed.GetString("Specify output sub-folder name");
            if (res2.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                return;*/
            try
            {
                //get parameter from input json
                var parameters = JsonConvert.DeserializeObject<Parameters>(File.ReadAllText(res1.StringResult));
                //Directory.CreateDirectory(res2.StringResult);
                //extract layer names and block names from drawing as requested and place the results in the
                //output folder
                var db = doc.Database;
                //build XML output
                JsonOutput output = new JsonOutput
                {
                    Blocks = new List<Block>(),
                    Dependents = new List<Dependent>(),
                    Layers = new List<Layer>()
                };
                if (parameters.ExtractLayerNames)
                {
                    using (var helper = new GeneralHelper())
                    {
                        var list = helper.GetLayers().ToList();
                        if (list.Count > 0)
                        {
                            list.ForEach(layer => {
                                var jsonlayer = new Layer();
                                jsonlayer.Name = layer.Name;
                                jsonlayer.Description = layer.Description;
                                output.Layers.Add(jsonlayer);
                            });
                        }
                    }
                }
                if (parameters.ExtractBlockNames)
                {
                    using (var helper = new GeneralHelper())
                    {
                        var list = helper.GetBlocks().ToList();
                        if (list.Count > 0)
                        {
                            list.ForEach(block => {
                                var jsonblock = new Block();
                                jsonblock.Name = block.Name;
                                jsonblock.Comments = block.Comments;
                                output.Blocks.Add(jsonblock);
                            });
                        }
                    }
                }
                if (parameters.ExtractDependents)
                {
                    TransmittalFile tf;
                    TransmittalOperation to = new TransmittalOperation();
                    TransmittalInfo ti = to.getTransmittalInfoInterface();
                    ti.includeDataLinkFile = 1;
                    ti.includeDGNUnderlay = 1;
                    ti.includeDWFUnderlay = 1;
                    ti.includeFontFile = 1;
                    ti.includeImageFile = 1;
                    ti.includeInventorProjectFile = 1;
                    ti.includeInventorReferences = 1;
                    ti.includeMaterialTextureFile = 1;
                    ti.includeNestedOverlayXrefDwg = 1;
                    ti.includePDFUnderlay = 1;
                    ti.includePhotometricWebFile = 1;
                    ti.includePlotFile = 1;
                    ti.includeUnloadedXrefDwg = 1;
                    ti.includeXrefDwg = 1;
                    if (to.addDrawingFile(doc.Name, out tf) == AddFileReturnVal.eFileAdded)
                    {
                        TransmittalFilesGraph tfg = to.getTransmittalGraphInterface();
                        TransmittalFile rootTF = tfg.getRoot();
                        DisplayDependent(rootTF, output);
                    }
                }
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "results.json"), JsonConvert.SerializeObject(output));
            }
            catch (System.Exception e)
            {
                ed.WriteMessage("Error: {0}", e);
            }
        }
    }
}
