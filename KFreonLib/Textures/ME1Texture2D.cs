﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AmaroK86.ImageFormat;
using Gibbed.IO;
using BitConverter = KFreonLib.Misc.BitConverter;
using System.Windows.Forms;
using KFreonLib.PCCObjects;
using KFreonLib.Helpers;
using KFreonLib.MEDirectories;
using KFreonLib.Debugging;
using CSharpImageLibrary.General;

namespace KFreonLib.Textures
{
    public class ME1Texture2D : ITexture2D
    {
        public enum storage
        {
            //arcCpr = 0x3, // archive compressed
            arcCpr = 0x11, //archive compressed (guessing)
            arcUnc = 0x1, // archive uncompressed (DLC)
            pccSto = 0x0, // pcc local storage
            empty = 0x21,  // unused image (void pointer sorta)
            pccCpr = 0x10 // pcc Compressed (only ME1)
        }

        public struct ImageInfo : IImageInfo
        {
            public storage storageType;
            public int cprSize { get; set; }

            public ImageSize imgSize
            {
                get;
                set;
            }

            public int offset
            {
                get;
                set;
            }

            public int GameVersion
            {
                get;
                set;
            }

            int IImageInfo.storageType
            {
                get
                {
                    return (int)storageType;
                }
                set
                {
                    storageType = (storage)value;
                }
            }


            public int uncSize
            {
                get;
                set;
            }

            public bool CompareStorage(string storage)
            {
                bool retval = false;
                switch (storage)
                {
                    case "pccSto":
                        retval = (int)ME1Texture2D.storage.pccSto == storageType;
                        break;
                    case "arcCpr":
                        retval = (int)ME1Texture2D.storage.arcCpr == (int)storageType;
                        break;
                    case "arcUnc":
                        retval = (int)ME1Texture2D.storage.arcUnc == (int)storageType;
                        break;
                    case "empty":
                        retval = (int)ME1Texture2D.storage.empty == (int)storageType;
                        break;
                }
                return retval;
            }
        }

        public const string className = "ME1Texture2D";
        public const string class2 = "LightMapTexture2D";
        public const string class3 = "TextureFlipBook";
        public byte[] headerData;
        public byte[] imageData;
        byte[] footerData;
        public Dictionary<string, SaltPropertyReader.Property> properties;
        public int exportOffset;
        public uint dataOffset = 0;
        private uint numMipMaps;
        public string Compression;
        public string FullPackage;
        public string oriPackage = null;
        public int UnpackNum;
        public String ListName;
        public String Class;

        // KFreon:  List so all files can be added rather than searched for
        public List<string> allFiles { get; set; }

        private int dims = 0;

        public ME1Texture2D(string name, List<string> pccs, List<int> ExpIDs, string pathBIOGame, int GameVersion, uint hash = 0, String listname = null)
        {
            hasChanged = false;
            texName = name;

            List<string> temppccs = new List<string>(pccs);
            List<int> tempexp = new List<int>(ExpIDs);
            //KFreonLib.PCCObjects.Misc.ReorderFiles(ref temppccs, ref tempexp, pathBIOGame, GameVersion);

            allPccs = temppccs;
            expIDs = tempexp;
            Hash = hash;
            ListName = listname;
            privateimgList = new List<ImageInfo>();
        }

        public ME1Texture2D(ME1PCCObject pcc, int pccExpID)
        {
            ME1ExportEntry exp = pcc.Exports[pccExpID]; ;
            Class = exp.ClassName;
            exportOffset = exp.DataOffset;
            FullPackage = exp.PackageFullName;
            texName = exp.ObjectName;
            allPccs = new List<string>();
            allPccs.Add(pcc.pccFileName);
            properties = new Dictionary<string, SaltPropertyReader.Property>();
            byte[] rawData = (byte[])exp.Data.Clone();
            Compression = "No Compression";
            int propertiesOffset = SaltPropertyReader.detectStart(pcc, rawData);
            headerData = new byte[propertiesOffset];
            Buffer.BlockCopy(rawData, 0, headerData, 0, propertiesOffset);
            pccOffset = (uint)exp.DataOffset;
            UnpackNum = 0;
            List<SaltPropertyReader.Property> tempProperties = SaltPropertyReader.getPropList(pcc, rawData);
            for (int i = 0; i < tempProperties.Count; i++)
            {
                SaltPropertyReader.Property property = tempProperties[i];
                if (property.Name == "UnpackMin")
                    UnpackNum++;
                if (!properties.ContainsKey(property.Name))
                    properties.Add(property.Name, property);

                switch (property.Name)
                {
                    case "Format": texFormat = property.Value.StringValue; break;
                    case "LODGroup": LODGroup = property.Value.StringValue; break;
                    case "CompressionSettings": Compression = property.Value.StringValue; break;
                    case "None": dataOffset = (uint)(property.offsetval + property.Size); break;
                }
            }

            // if "None" property isn't found throws an exception
            if (dataOffset == 0)
                throw new Exception("\"None\" property not found");
            else
            {
                imageData = new byte[rawData.Length - dataOffset];
                Buffer.BlockCopy(rawData, (int)dataOffset, imageData, 0, (int)(rawData.Length - dataOffset));
            }
            //DebugOutput.PrintLn("ImageData size = " + imageData.Length);
            pccExpIdx = pccExpID;

            MemoryStream dataStream = new MemoryStream(imageData);
            privateimgList = new List<ImageInfo>();
            dataStream.ReadValueU32(); //Current position in pcc
            numMipMaps = dataStream.ReadValueU32();
            uint count = numMipMaps;
            //DebugOutput.PrintLn(numMipMaps + " derp");
            while (dataStream.Position < dataStream.Length && count > 0)
            {
                ImageInfo imgInfo = new ImageInfo();
                imgInfo.storageType = (storage)dataStream.ReadValueS32();
                imgInfo.uncSize = dataStream.ReadValueS32();
                imgInfo.cprSize = dataStream.ReadValueS32();
                imgInfo.offset = dataStream.ReadValueS32();
                if (imgInfo.storageType == storage.pccSto)
                {
                    imgInfo.offset = (int)dataStream.Position;
                    dataStream.Seek(imgInfo.uncSize, SeekOrigin.Current);
                }
                else if (imgInfo.storageType == storage.pccCpr)
                {
                    imgInfo.offset = (int)dataStream.Position;
                    dataStream.Seek(imgInfo.cprSize, SeekOrigin.Current);
                }

                imgInfo.imgSize = new ImageSize(dataStream.ReadValueU32(), dataStream.ReadValueU32());
                if (privateimgList.Exists(img => img.imgSize == imgInfo.imgSize))
                {
                    uint width = imgInfo.imgSize.width;
                    uint height = imgInfo.imgSize.height;
                    if (width == 4 && privateimgList.Exists(img => img.imgSize.width == width))
                        width = privateimgList.Last().imgSize.width / 2;
                    if (width == 0)
                        width = 1;
                    if (height == 4 && privateimgList.Exists(img => img.imgSize.height == height))
                        height = privateimgList.Last().imgSize.height / 2;
                    if (height == 0)
                        height = 1;
                    imgInfo.imgSize = new ImageSize(width, height);
                    if (privateimgList.Exists(img => img.imgSize == imgInfo.imgSize))
                        throw new Exception("Duplicate image size found");
                }
                privateimgList.Add(imgInfo);
                count--;
                //DebugOutput.PrintLn("ImgInfo no: " + count + ", Storage Type = " + imgInfo.storageType + ", offset = " + imgInfo.offset);
            }
            dataStream.Seek(-4, SeekOrigin.End);
            footerData = dataStream.ReadBytes(4);
            dataStream.Dispose();
        }

        public string getFileFormat()
        {
            return ".dds";
        }

        public byte[] extractImage(ImageInfo imgInfo, bool NoOutput, string archiveDir = null, string fileName = null)
        {
            ImageFile imgFile;
            if (fileName == null)
                fileName = texName + "_" + imgInfo.imgSize + getFileFormat();

            byte[] imgBuffer = null;

            switch (imgInfo.storageType)
            {
                case storage.pccSto:
                    imgBuffer = new byte[imgInfo.uncSize];
                    Buffer.BlockCopy(imageData, imgInfo.offset, imgBuffer, 0, imgInfo.uncSize);
                    break;
                case storage.arcCpr:
                case storage.arcUnc:
                    string archivePath = FindFile();
                    if (String.IsNullOrEmpty(archivePath))
                        throw new FileNotFoundException();
                    ME1PCCObject temp = new ME1PCCObject(archivePath);
                    for (int i = 0; i < temp.ExportCount; i++)
                    {
                        if (String.Compare(texName, temp.Exports[i].ObjectName, true) == 0 && temp.Exports[i].ValidTextureClass())
                        {
                            ME1Texture2D temptex = new ME1Texture2D(temp, i);
                            imgBuffer = temptex.extractImage(imgInfo.imgSize.ToString(), NoOutput, null, fileName);
                            //                            temptex.extractImage(imgInfo.imgSize.ToString(), temp, null, fileName);
                        }
                    }
                    break;
                case storage.pccCpr:
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        SaltLZOHelper lzohelp = new SaltLZOHelper();
                        imgBuffer = lzohelp.DecompressTex(ms, imgInfo.offset, imgInfo.uncSize, imgInfo.cprSize);
                    }
                    break;
                default:
                    throw new FormatException("Unsupported texture storage type");
            }

            if (imgInfo.storageType == storage.pccSto || imgInfo.storageType == storage.pccCpr)
            {
                if (getFileFormat() == ".dds")
                    imgFile = new DDS(fileName, imgInfo.imgSize, texFormat, imgBuffer);
                else
                    imgFile = new TGA(fileName, imgInfo.imgSize, texFormat, imgBuffer);

                byte[] saveImg = imgFile.ToArray();

                if (!NoOutput)
                    using (FileStream outputImg = new FileStream(imgFile.fileName, FileMode.Create, FileAccess.Write))
                        outputImg.Write(saveImg, 0, saveImg.Length);
                return saveImg;
            }
            return imgBuffer;
        }

        public byte[] extractImage(string strImgSize, bool NoOutput, string archiveDir = null, string fileName = null)
        {
            ImageSize imgSize = ImageSize.stringToSize(strImgSize);
            byte[] retval;
            if (privateimgList.Exists(img => img.imgSize == imgSize))
                retval = extractImage(privateimgList.Find(img => img.imgSize == imgSize), NoOutput, archiveDir, fileName);
            else
                throw new FileNotFoundException("Image with resolution " + imgSize + " not found");
            return retval;
        }

        public byte[] ToArray(int pccExportDataOffset, ME1PCCObject pcc)
        {
            MemoryStream buffer = new MemoryStream();
            buffer.Write(headerData, 0, headerData.Length);

            if (properties.ContainsKey("LODGroup"))
            {
                properties["LODGroup"].Value.StringValue = "TEXTUREGROUP_LightAndShadowMap";
                //properties["LODGroup"].Value.IntValue = 1025;
            }
            else
            {
                buffer.WriteValueS64(pcc.AddName("LODGroup"));
                buffer.WriteValueS64(pcc.AddName("ByteProperty"));
                buffer.WriteValueS64(8);
                buffer.WriteValueS32(pcc.AddName("TEXTUREGROUP_LightAndShadowMap"));
                buffer.WriteValueS32(1025);
            }

            foreach (KeyValuePair<string, SaltPropertyReader.Property> kvp in properties)
            {
                SaltPropertyReader.Property prop = kvp.Value;

                if (prop.Name == "UnpackMin")
                {
                    for (int j = 0; j < UnpackNum; j++)
                    {
                        buffer.WriteValueS64(pcc.AddName(prop.Name));
                        buffer.WriteValueS64(pcc.AddName(prop.TypeVal.ToString()));
                        buffer.WriteValueS32(prop.Size);
                        buffer.WriteValueS32(j);
                        buffer.WriteValueF32(prop.Value.FloatValue, Endian.Little);
                    }
                    continue;
                }

                buffer.WriteValueS64(pcc.AddName(prop.Name));
                if (prop.Name == "None")
                {
                    for (int j = 0; j < 12; j++)
                        buffer.WriteByte(0);
                }
                else
                {
                    buffer.WriteValueS64(pcc.AddName(prop.TypeVal.ToString()));
                    buffer.WriteValueS64(prop.Size);

                    switch (prop.TypeVal)
                    {
                        case SaltPropertyReader.Type.IntProperty:
                            buffer.WriteValueS32(prop.Value.IntValue);
                            break;
                        case SaltPropertyReader.Type.BoolProperty:
                            buffer.Seek(-4, SeekOrigin.Current);
                            buffer.WriteValueS32(prop.Value.IntValue);
                            buffer.Seek(4, SeekOrigin.Current);
                            break;
                        case SaltPropertyReader.Type.NameProperty:
                            buffer.WriteValueS64(pcc.AddName(prop.Value.StringValue));
                            // Heff: Modified to handle name references.
                            //var index = pcc.AddName(prop.Value.StringValue);
                            //buffer.WriteValueS32(index);
                            //buffer.WriteValueS32(prop.Value.NameValue.count);
                            break;
                        case SaltPropertyReader.Type.StrProperty:
                            buffer.WriteValueS32(prop.Value.StringValue.Length + 1);
                            foreach (char c in prop.Value.StringValue)
                                buffer.WriteByte((byte)c);
                            buffer.WriteByte(0);
                            break;
                        case SaltPropertyReader.Type.StructProperty:
                            buffer.WriteValueS64(pcc.AddName(prop.Value.StringValue));
                            foreach (SaltPropertyReader.PropertyValue value in prop.Value.Array)
                                buffer.WriteValueS32(value.IntValue);
                            break;
                        case SaltPropertyReader.Type.ByteProperty:
                            buffer.WriteValueS32(pcc.AddName(prop.Value.StringValue));
                            buffer.WriteValueS32(prop.Value.IntValue);
                            break;
                        case SaltPropertyReader.Type.FloatProperty:
                            buffer.WriteValueF32(prop.Value.FloatValue, Endian.Little);
                            break;
                        default:
                            throw new FormatException("unknown property");
                    }
                }
            }

            buffer.WriteValueS32((int)(pccOffset + buffer.Position + 4));

            //Remove empty textures
            List<ImageInfo> tempList = new List<ImageInfo>();
            foreach (ImageInfo imgInfo in privateimgList)
            {
                if (imgInfo.storageType != storage.empty)
                    tempList.Add(imgInfo);
            }
            privateimgList = tempList;
            numMipMaps = (uint)privateimgList.Count;

            buffer.WriteValueU32(numMipMaps);

            foreach (ImageInfo imgInfo in privateimgList)
            {
                buffer.WriteValueS32((int)imgInfo.storageType);
                buffer.WriteValueS32(imgInfo.uncSize);
                buffer.WriteValueS32(imgInfo.cprSize);
                if (imgInfo.storageType == storage.pccSto)
                {
                    buffer.WriteValueS32((int)(imgInfo.offset + pccExportDataOffset + dataOffset));
                    buffer.Write(imageData, imgInfo.offset, imgInfo.uncSize);
                }
                else if (imgInfo.storageType == storage.pccCpr)
                {
                    buffer.WriteValueS32((int)(imgInfo.offset + pccExportDataOffset + dataOffset));
                    buffer.Write(imageData, imgInfo.offset, imgInfo.cprSize);
                }
                else
                    buffer.WriteValueS32(imgInfo.offset);
                if (imgInfo.imgSize.width < 4)
                    buffer.WriteValueU32(4);
                else
                    buffer.WriteValueU32(imgInfo.imgSize.width);
                if (imgInfo.imgSize.height < 4)
                    buffer.WriteValueU32(4);
                else
                    buffer.WriteValueU32(imgInfo.imgSize.height);
            }
            buffer.WriteBytes(footerData);
            return buffer.ToArray();
        }


        public void replaceImage(string strImgSize, ImageFile im, string archiveDir = null)
        {
            replaceImage(strImgSize, im, true, archiveDir);
        }

        public void replaceImage(string strImgSize, ImageFile im, bool enforceSize, string archiveDir = null)
        {
            ImageSize imgSize = ImageSize.stringToSize(strImgSize);
            if (!privateimgList.Exists(img => img.imgSize == imgSize))
                throw new FileNotFoundException("Image with resolution " + imgSize + " isn't found");

            int imageIdx = privateimgList.FindIndex(img => img.imgSize == imgSize);
            ImageInfo imgInfo = privateimgList[imageIdx];

            // check if replacing image is supported
            ImageFile imgFile = im;

            // Heff: Made this check optional to allow for replacing with larger images.
            if (enforceSize && (imgFile.imgSize.height != imgInfo.imgSize.height || imgFile.imgSize.width != imgInfo.imgSize.width))
                throw new FormatException("Incorrect input texture dimensions. Expected: " + imgInfo.imgSize.ToString());

            // check if images have same format type
            if (!Methods.CheckTextureFormat(texFormat, imgFile.format))
                throw new FormatException("Different image format, original is " + texFormat + ", new is " + imgFile.subtype());

            byte[] imgBuffer;

            // if the image is empty then recover the archive compression from the image list
            if (imgInfo.storageType == storage.empty)
            {
                imgInfo.storageType = privateimgList.Find(img => img.storageType != storage.empty && img.storageType != storage.pccSto).storageType;
                imgInfo.uncSize = imgFile.resize().Length;
                imgInfo.cprSize = imgFile.resize().Length;
            }

            switch (imgInfo.storageType)
            {
                case storage.arcCpr:
                case storage.arcUnc:
                    throw new NotImplementedException("Texture replacement not supported in external packages yet");
                case storage.pccSto:
                    imgBuffer = imgFile.resize();
                    using (MemoryStream dataStream = new MemoryStream())
                    {
                        dataStream.WriteBytes(imageData);
                        if (imgBuffer.Length <= imgInfo.uncSize && imgInfo.offset > 0)
                            dataStream.Seek(imgInfo.offset, SeekOrigin.Begin);
                        else
                            imgInfo.offset = (int)dataStream.Position;
                        dataStream.WriteBytes(imgBuffer);
                        imgInfo.cprSize = imgBuffer.Length;
                        imgInfo.uncSize = imgBuffer.Length;
                        imageData = dataStream.ToArray();
                    }
                    break;
                case storage.pccCpr:
                    using (MemoryStream dataStream = new MemoryStream())
                    {
                        dataStream.WriteBytes(imageData);
                        SaltLZOHelper lzohelper = new SaltLZOHelper();
                        imgBuffer = lzohelper.CompressTex(imgFile.resize());
                        if (imgBuffer.Length <= imgInfo.cprSize && imgInfo.offset > 0)
                            dataStream.Seek(imgInfo.offset, SeekOrigin.Begin);
                        else
                            imgInfo.offset = (int)dataStream.Position;
                        dataStream.WriteBytes(imgBuffer);
                        imgInfo.cprSize = imgBuffer.Length;
                        imgInfo.uncSize = imgFile.resize().Length;
                        imageData = dataStream.ToArray();
                    }
                    break;
            }

            privateimgList[imageIdx] = imgInfo;
        }

        public void OneImageToRuleThemAll(ImageFile im, byte[] imgData)
        {
            ImageMipMapHandler imgMipMap = new ImageMipMapHandler("", imgData);

            if (Class == class2 || Class == class3)
                ChangeFormat(imgMipMap.imageList[0].format);

            // starts from the smaller image
            for (int i = imgMipMap.imageList.Count - 1; i >= 0; i--)
            {
                ImageFile newImageFile = imgMipMap.imageList[i];

                if (!Methods.CheckTextureFormat(texFormat, newImageFile.format))
                    throw new FormatException("Different image format, original is " + texFormat + ", new is " + newImageFile.subtype());

                // if the image size exists inside the ME1Texture2D image list then we have to replace it
                if (privateimgList.Exists(img => img.imgSize == newImageFile.imgSize))
                {
                    // ...but at least for now I can reuse my replaceImage function... ;)
                    replaceImage(newImageFile.imgSize.ToString(), newImageFile);
                }
                else if (newImageFile.imgSize.width > privateimgList[0].imgSize.width) // if the image doesn't exists then we have to add it
                {
                    // ...and use my addBiggerImage function! :P
                    addBiggerImage(newImageFile);
                }
                // else ignore the image
            }

            // Remove higher res versions and fix up properties
            while (privateimgList[0].imgSize.width > imgMipMap.imageList[0].imgSize.width)
            {
                privateimgList.RemoveAt(0);
                numMipMaps--;
            }
            if (properties.ContainsKey("SizeX"))
                properties["SizeX"].Value.IntValue = (int)privateimgList[0].imgSize.width;
            if (properties.ContainsKey("SizeY"))
                properties["SizeY"].Value.IntValue = (int)privateimgList[0].imgSize.height;
            if (properties.ContainsKey("MipTailBaseIdx"))
                properties["MipTailBaseIdx"].Value.IntValue = privateimgList.Count + 1;
        }

        public void addMissingImage(String strImgSize, string fileToReplace)
        {
            if (privateimgList.Count == 1)
                throw new Exception("The imglist must contain more than 1 texture");

            ImageSize imgSize = ImageSize.stringToSize(strImgSize);
            if (privateimgList.Exists(img => img.imgSize.width == imgSize.width && img.imgSize.height == imgSize.height))
                throw new Exception("The img already exists in the list");
            ImageInfo tempImg = privateimgList.Last();

            if (!File.Exists(fileToReplace))
                throw new FileNotFoundException("Required file was not found");

            ImageFile dds = new DDS(fileToReplace, null);

            if (dds.imgSize.width != imgSize.width || dds.imgSize.height != imgSize.height)
                throw new FormatException("Input texture is not required size");

            if (dds.format == "R8G8B8")
            {
                byte[] buff = ImageMipMapHandler.ConvertTo32bit(dds.imgData, (int)dds.imgSize.width, (int)dds.imgSize.height);
                dds = new DDS(null, dds.imgSize, "A8R8G8B8", buff);
            }

            if (texFormat == "PF_NormalMap_HQ")
            {
                if (dds.format != "ATI2")
                    throw new FormatException("Input texture is the wrong format");
            }
            else if (String.Compare(texFormat, "PF_" + dds.format, true) != 0 && String.Compare(texFormat, dds.format, true) != 0)
                throw new FormatException("Input texture is the wrong format");

            ImageInfo newImg = new ImageInfo();

            if (tempImg.storageType == storage.empty || tempImg.storageType == storage.arcCpr || tempImg.storageType == storage.arcUnc)
                throw new FormatException("Existing textures cannot be empty or externally stored");
            newImg.storageType = tempImg.storageType;
            newImg.imgSize = imgSize;

            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteBytes(imageData);
                newImg.offset = (int)ms.Position;
                switch (newImg.storageType)
                {
                    case storage.pccSto:
                        ms.WriteBytes(dds.resize());
                        //newImg.cprSize = dds.imgData.Length;
                        //newImg.uncSize = dds.imgData.Length;
                        newImg.cprSize = dds.resize().Length;
                        newImg.uncSize = dds.resize().Length;
                        break;
                    case storage.pccCpr:
                        SaltLZOHelper lzohelper = new SaltLZOHelper();
                        //byte[] buff = lzohelper.CompressTex(dds.imgData);
                        byte[] buff = lzohelper.CompressTex(dds.resize());
                        ms.WriteBytes(buff);
                        //newImg.uncSize = dds.imgData.Length;
                        newImg.uncSize = dds.resize().Length;
                        newImg.cprSize = buff.Length;
                        break;
                }
                imageData = ms.ToArray();
            }

            int i = 0;
            for (; i < privateimgList.Count; i++)
            {
                if (privateimgList[i].imgSize.width > imgSize.width)
                    continue;

                privateimgList.Insert(i, newImg);
                return;
            }
            privateimgList.Insert(i, newImg);

            if (ImageMipMapHandler.CprFormat(dds.format) && (newImg.imgSize.width < 4 || newImg.imgSize.height < 4))
            {
                newImg = privateimgList[i];
                if (newImg.imgSize.width < 4 && newImg.imgSize.height > 4)
                {
                    newImg.imgSize = new ImageSize(4, newImg.imgSize.height);
                }
                else if (newImg.imgSize.width > 4 && newImg.imgSize.height < 4)
                {
                    newImg.imgSize = new ImageSize(newImg.imgSize.width, 4);
                }
                else if (newImg.imgSize.width < 4 && newImg.imgSize.height < 4)
                {
                    newImg.imgSize = new ImageSize(4, 4);
                }
                else
                    throw new Exception("safety catch");
                privateimgList[i] = newImg;
            }
            //throw new Exception("Newimg wasn't inserted in list!");
        }

        public void addBiggerImage(ImageFile im)
        {
            ImageSize biggerImageSizeOnList = privateimgList.Max(image => image.imgSize);
            // check if replacing image is supported
            ImageFile imgFile = im;


            if (imgFile.format == "R8G8B8")
            {
                byte[] buff = ImageMipMapHandler.ConvertTo32bit(imgFile.imgData, (int)imgFile.imgSize.width, (int)imgFile.imgSize.height);
                imgFile = new DDS(null, imgFile.imgSize, "A8R8G8B8", buff);
            }

            if (!Methods.CheckTextureFormat(texFormat, imgFile.format))
                throw new FormatException("Different image format, original is " + texFormat + ", new is " + imgFile.subtype());

            // check if image to add is valid
            if (biggerImageSizeOnList.width * 2 != imgFile.imgSize.width || biggerImageSizeOnList.height * 2 != imgFile.imgSize.height)
                throw new FormatException("image size " + imgFile.imgSize + " isn't valid, must be " + new ImageSize(biggerImageSizeOnList.width * 2, biggerImageSizeOnList.height * 2));

            // this check avoids insertion inside textures that have only 1 image stored inside pcc
            //if (!imgList.Exists(img => img.storageType != storage.empty && img.storageType != storage.pccSto))
            //    throw new Exception("Unable to add image, texture must have a reference to an external archive");
            if (privateimgList.Count <= 1)
                throw new Exception("Unable to add image, texture must have more than one image present");

            // !!! warning, this method breaks consistency between imgList and imageData[] !!!
            ImageInfo newImgInfo = new ImageInfo();
            newImgInfo.storageType = privateimgList.Find(img => img.storageType != storage.empty && img.storageType != storage.pccSto).storageType;
            newImgInfo.imgSize = imgFile.imgSize;
            newImgInfo.uncSize = imgFile.resize().Length;
            newImgInfo.cprSize = 0x00; // not yet filled
            newImgInfo.offset = 0x00; // not yet filled
            privateimgList.Insert(0, newImgInfo); // insert new image on top of the list
            //now I let believe the program that I'm doing an image replace, saving lot of code ;)
            replaceImage(newImgInfo.imgSize.ToString(), im);

            //updating num of images
            numMipMaps++;

            // update MipTailBaseIdx
            //SaltPropertyReader.Property MipTail = properties["MipTailBaseIdx"];
            int propVal = properties["MipTailBaseIdx"].Value.IntValue;
            propVal++;
            properties["MipTailBaseIdx"].Value.IntValue = propVal;
            //MessageBox.Show("raw size: " + properties["MipTailBaseIdx"].raw.Length + "\nproperty offset: " + properties["MipTailBaseIdx"].offsetval);
            using (MemoryStream rawStream = new MemoryStream(properties["MipTailBaseIdx"].raw))
            {
                rawStream.Seek(rawStream.Length - 4, SeekOrigin.Begin);
                rawStream.WriteValueS32(propVal);
                properties["MipTailBaseIdx"].raw = rawStream.ToArray();
            }
            //properties["MipTailBaseIdx"] = MipTail;

            // update Sizes
            //SaltPropertyReader.Property Size = properties["SizeX"];
            propVal = (int)newImgInfo.imgSize.width;
            properties["SizeX"].Value.IntValue = propVal;
            using (MemoryStream rawStream = new MemoryStream(properties["SizeX"].raw))
            {
                rawStream.Seek(rawStream.Length - 4, SeekOrigin.Begin);
                rawStream.WriteValueS32(propVal);
                properties["SizeX"].raw = rawStream.ToArray();
            }
            //properties["SizeX"] = Size;
            //Size = properties["SizeY"];
            properties["SizeY"].Value.IntValue = (int)newImgInfo.imgSize.height;
            using (MemoryStream rawStream = new MemoryStream(properties["SizeY"].raw))
            {
                rawStream.Seek(rawStream.Length - 4, SeekOrigin.Begin);
                rawStream.WriteValueS32(propVal);
                properties["SizeY"].raw = rawStream.ToArray();
            }
            //properties["SizeY"] = Size;
            //this.hasChanged = true;
        }

        public void HardReplaceImage(string strImgSize, string fileToReplace)
        {
            if (!File.Exists(fileToReplace))
                throw new FileNotFoundException("Required file was not found");

            ImageFile dds = new DDS(fileToReplace, null);

            if (strImgSize == null)
                strImgSize = dds.imgSize.ToString();

            if (privateimgList.Count != 1)
                throw new Exception("Cannot use this function for a multi-level texture");

            ImageSize imgSize = ImageSize.stringToSize(strImgSize);
            // Check for correct dimensions/ size
            if (imgSize.width == privateimgList[0].imgSize.width)
            {
                if (imgSize.height != privateimgList[0].imgSize.height)
                    throw new FormatException("The input size is not the correct dimensions (Error: 0)");
            }
            else if (imgSize.width < privateimgList[0].imgSize.width)
            {
                if (privateimgList[0].imgSize.width / imgSize.width != privateimgList[0].imgSize.height / imgSize.height || (privateimgList[0].imgSize.width / imgSize.width) % 2 != 0)
                    throw new FormatException("The input size is not the correct size (Error: 1)");
            }
            else
            {
                if (imgSize.width / privateimgList[0].imgSize.width != imgSize.height / privateimgList[0].imgSize.height || (imgSize.width / privateimgList[0].imgSize.width) % 2 != 0)
                    throw new FormatException("The input size is not the correct size (Error: 2)");
            }

            if (dds.format == "R8G8B8")
            {
                byte[] buff = ImageMipMapHandler.ConvertTo32bit(dds.imgData, (int)dds.imgSize.width, (int)dds.imgSize.height);
                dds = new DDS(null, dds.imgSize, "A8R8G8B8", buff);
            }

            if (Class == class2 || Class == class3)
                ChangeFormat(dds.format);

            if (texFormat == "PF_NormalMap_HQ")
            {
                if (dds.format != "ATI2")
                    throw new FormatException("Input texture is the wrong format");
            }
            else if (String.Compare(texFormat, "PF_" + dds.format, true) != 0 && String.Compare(texFormat, dds.format, true) != 0)
                throw new FormatException("Input texture is the wrong format");

            ImageInfo newImg = new ImageInfo();
            newImg.storageType = privateimgList[0].storageType;
            if (newImg.storageType == storage.empty || newImg.storageType == storage.arcCpr || newImg.storageType == storage.arcUnc)
                throw new FormatException("Original texture cannot be empty or externally stored");

            newImg.offset = 0;
            newImg.imgSize = imgSize;

            switch (newImg.storageType)
            {
                case storage.pccSto:
                    imageData = dds.resize();
                    newImg.cprSize = imageData.Length;
                    newImg.uncSize = imageData.Length;
                    break;
                case storage.pccCpr:
                    SaltLZOHelper lzohelper = new SaltLZOHelper();
                    imageData = lzohelper.CompressTex(dds.resize());
                    newImg.cprSize = imageData.Length;
                    newImg.uncSize = dds.resize().Length;
                    break;
            }

            privateimgList.RemoveAt(0);
            privateimgList.Add(newImg);

            // Fix up properties
            properties["SizeX"].Value.IntValue = (int)imgSize.width;
            properties["SizeY"].Value.IntValue = (int)imgSize.height;
        }

        public void DumpImageData(ImageInfo imgInfo, string archiveDir = null, string fileName = null)
        {
            if (fileName == null)
                fileName = texName + "_" + imgInfo.imgSize + ".bin";

            byte[] imgBuffer;

            switch (imgInfo.storageType)
            {
                case storage.pccSto:
                    imgBuffer = new byte[imgInfo.uncSize];
                    Buffer.BlockCopy(imageData, imgInfo.offset, imgBuffer, 0, imgInfo.uncSize);
                    break;
                case storage.pccCpr:
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        SaltLZOHelper lzohelp = new SaltLZOHelper();
                        imgBuffer = lzohelp.DecompressTex(ms, imgInfo.offset, imgInfo.uncSize, imgInfo.cprSize);
                    }
                    break;
                case storage.arcCpr:
                case storage.arcUnc:
                    string archivePath = FindFile();
                    if (!File.Exists(archivePath))
                        throw new FileNotFoundException("Texture archive not found in " + archivePath);

                    using (FileStream archiveStream = File.OpenRead(archivePath))
                    {
                        if (imgInfo.storageType == storage.arcCpr)
                        {
                            SaltLZOHelper lzohelp = new SaltLZOHelper();
                            imgBuffer = lzohelp.DecompressTex(archiveStream, imgInfo.offset, imgInfo.uncSize, imgInfo.cprSize);
                        }
                        else
                        {
                            archiveStream.Seek(imgInfo.offset, SeekOrigin.Begin);
                            imgBuffer = new byte[imgInfo.uncSize];
                            archiveStream.Read(imgBuffer, 0, imgBuffer.Length);
                        }
                    }
                    break;
                default:
                    throw new FormatException("Unsupported texture storage type");
            }
            using (FileStream outputImg = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                outputImg.Write(imgBuffer, 0, imgBuffer.Length);
        }

        public void DumpImage(string strImgSize, string archiveDir = null, string fileName = null)
        {
            ImageSize imgSize = ImageSize.stringToSize(strImgSize);
            if (privateimgList.Exists(img => img.imgSize == imgSize))
                DumpImageData(privateimgList.Find(img => img.imgSize == imgSize), archiveDir, fileName);
            else
                throw new FileNotFoundException("Image with resolution " + imgSize + " not found");
        }

        public void ChangeTexFormat(string newFormat, ME1PCCObject pcc)
        {
            SaltPropertyReader.Property prop = properties["Format"];
            Int64 formatID = (Int64)pcc.AddName(newFormat);
            byte[] buff = BitConverter.GetBytes(formatID);
            Buffer.BlockCopy(buff, 0, prop.raw, 24, sizeof(Int64));
            prop.Value.StringValue = pcc.Names[(int)formatID];
            properties["Format"] = prop;
            texFormat = properties["Format"].Value.StringValue;
        }

        public void ChangeCompression(string newComp, ME1PCCObject pcc)
        {
            if (!properties.ContainsKey("CompressionSettings"))
                throw new KeyNotFoundException("Texture doesn't have a compression property");
            SaltPropertyReader.Property prop = properties["CompressionSettings"];
            Int64 comp = (Int64)pcc.AddName(newComp);
            byte[] buff = BitConverter.GetBytes(comp);
            Buffer.BlockCopy(buff, 0, prop.raw, 24, sizeof(Int64));
            prop.Value.StringValue = pcc.Names[(int)comp];
            properties["CompressionSettings"] = prop;
            Compression = properties["CompressionSettings"].Value.StringValue;

        }

        public void CopyImgList(ME1Texture2D inTex, ME1PCCObject pcc, bool norender = false)
        {
            List<ImageInfo> tempList = new List<ImageInfo>();
            MemoryStream tempData = new MemoryStream();
            SaltLZOHelper lzo = new SaltLZOHelper();
            numMipMaps = inTex.numMipMaps;

            // forced norenderfix
            // norender = true;

            int type = -1;
            if (!norender)
            {
                if (privateimgList.Exists(img => img.storageType == storage.arcCpr) && privateimgList.Count > 1)
                    type = 1;
                else if (privateimgList.Exists(img => img.storageType == storage.pccCpr))
                    type = 2;
                else if (privateimgList.Exists(img => img.storageType == storage.pccSto) || privateimgList.Count == 1)
                    type = 3;
            }
            else
                type = 3;

            switch (type)
            {
                case 1:
                    for (int i = 0; i < inTex.privateimgList.Count; i++)
                    {
                        try
                        {
                            ImageInfo newImg = new ImageInfo();
                            ImageInfo replaceImg = inTex.privateimgList[i];
                            ME1Texture2D.storage replaceType = privateimgList.Find(img => img.imgSize == replaceImg.imgSize).storageType;

                            int j = 0;
                            while (replaceType == storage.empty)
                            {
                                j++;
                                replaceType = privateimgList[privateimgList.FindIndex(img => img.imgSize == replaceImg.imgSize) + j].storageType;
                            }

                            if (replaceType == storage.arcCpr || !privateimgList.Exists(img => img.imgSize == replaceImg.imgSize))
                            {
                                newImg.storageType = storage.arcCpr;
                                newImg.uncSize = replaceImg.uncSize;
                                newImg.cprSize = replaceImg.cprSize;
                                newImg.imgSize = replaceImg.imgSize;
                                newImg.offset = (int)(replaceImg.offset + inTex.pccOffset + inTex.dataOffset);
                            }
                            else
                            {
                                newImg.storageType = storage.pccSto;
                                newImg.uncSize = replaceImg.uncSize;
                                newImg.cprSize = replaceImg.uncSize;
                                newImg.imgSize = replaceImg.imgSize;
                                newImg.offset = (int)(tempData.Position);
                                using (MemoryStream tempStream = new MemoryStream(inTex.imageData))
                                {
                                    tempData.WriteBytes(lzo.DecompressTex(tempStream, replaceImg.offset, replaceImg.uncSize, replaceImg.cprSize));
                                }
                            }
                            tempList.Add(newImg);
                        }
                        catch
                        {
                            ImageInfo replaceImg = inTex.privateimgList[i];
                            if (!privateimgList.Exists(img => img.imgSize == replaceImg.imgSize))
                                throw new Exception("An error occurred during imglist copying and no suitable replacement was found");
                            ImageInfo newImg = privateimgList.Find(img => img.imgSize == replaceImg.imgSize);
                            if (newImg.storageType != storage.pccCpr && newImg.storageType != storage.pccSto)
                                throw new Exception("An error occurred during imglist copying and no suitable replacement was found");
                            int temppos = newImg.offset;
                            newImg.offset = (int)tempData.Position;
                            tempData.Write(imageData, temppos, newImg.cprSize);
                            tempList.Add(newImg);
                        }
                    }
                    break;
                case 2:
                    for (int i = 0; i < inTex.privateimgList.Count; i++)
                    {
                        ImageInfo newImg = new ImageInfo();
                        ImageInfo replaceImg = inTex.privateimgList[i];
                        newImg.storageType = storage.pccCpr;
                        newImg.uncSize = replaceImg.uncSize;
                        newImg.cprSize = replaceImg.cprSize;
                        newImg.imgSize = replaceImg.imgSize;
                        newImg.offset = (int)(tempData.Position);
                        byte[] buffer = new byte[newImg.cprSize];
                        Buffer.BlockCopy(inTex.imageData, replaceImg.offset, buffer, 0, buffer.Length);
                        tempData.WriteBytes(buffer);
                        tempList.Add(newImg);
                    }
                    break;
                case 3:
                    for (int i = 0; i < inTex.privateimgList.Count; i++)
                    {
                        ImageInfo newImg = new ImageInfo();
                        ImageInfo replaceImg = inTex.privateimgList[i];
                        newImg.storageType = storage.pccSto;
                        newImg.uncSize = replaceImg.uncSize;
                        newImg.cprSize = replaceImg.uncSize;
                        newImg.imgSize = replaceImg.imgSize;
                        newImg.offset = (int)(tempData.Position);
                        if (replaceImg.storageType == storage.pccCpr)
                        {
                            using (MemoryStream tempStream = new MemoryStream(inTex.imageData))
                            {
                                tempData.WriteBytes(lzo.DecompressTex(tempStream, replaceImg.offset, replaceImg.uncSize, replaceImg.cprSize));
                            }
                        }
                        else if (replaceImg.storageType == storage.pccSto)
                        {
                            byte[] buffer = new byte[newImg.cprSize];
                            Buffer.BlockCopy(inTex.imageData, replaceImg.offset, buffer, 0, buffer.Length);
                            tempData.WriteBytes(buffer);
                        }
                        else
                            throw new NotImplementedException("Copying from non package stored texture no available");
                        tempList.Add(newImg);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            for (int i = 0; i < tempList.Count; i++)
            {
                ImageInfo tempinfo = tempList[i];
                if (inTex.privateimgList[i].storageType == storage.empty)
                    tempinfo.storageType = storage.empty;
                tempList[i] = tempinfo;
            }

            privateimgList = tempList;
            imageData = tempData.ToArray();
            tempData.Close();

            byte[] buff;
            //Copy properties
            using (MemoryStream tempMem = new MemoryStream())
            {
                tempMem.WriteBytes(headerData);
                for (int i = 0; i < inTex.properties.Count; i++)
                {
                    SaltPropertyReader.Property prop = inTex.properties.ElementAt(i).Value;

                    if (prop.Name == "UnpackMin")
                    {
                        for (int j = 0; j < inTex.UnpackNum; j++)
                        {
                            tempMem.WriteValueS64(pcc.AddName(prop.Name));
                            tempMem.WriteValueS64(pcc.AddName(prop.TypeVal.ToString()));
                            tempMem.WriteValueS32(prop.Size);
                            tempMem.WriteValueS32(j);
                            tempMem.WriteValueF32(prop.Value.FloatValue, Endian.Little);
                        }
                        continue;
                    }

                    tempMem.WriteValueS64(pcc.AddName(prop.Name));
                    if (prop.Name == "None")
                    {
                        for (int j = 0; j < 12; j++)
                            tempMem.WriteByte(0);
                    }
                    else
                    {
                        tempMem.WriteValueS64(pcc.AddName(prop.TypeVal.ToString()));
                        tempMem.WriteValueS64(prop.Size);

                        switch (prop.TypeVal)
                        {
                            case SaltPropertyReader.Type.IntProperty:
                                tempMem.WriteValueS32(prop.Value.IntValue);
                                break;
                            case SaltPropertyReader.Type.BoolProperty:
                                tempMem.Seek(-4, SeekOrigin.Current);
                                tempMem.WriteValueS32(prop.Value.IntValue);
                                tempMem.Seek(4, SeekOrigin.Current);
                                break;
                            case SaltPropertyReader.Type.NameProperty:
                                tempMem.WriteValueS64(pcc.AddName(prop.Value.StringValue));
                                // Heff: Modified to handle name references.
                                //var index = pcc.AddName(prop.Value.StringValue);
                                //tempMem.WriteValueS32(index);
                                //tempMem.WriteValueS32(prop.Value.NameValue.count);
                                break;
                            case SaltPropertyReader.Type.StrProperty:
                                tempMem.WriteValueS32(prop.Value.StringValue.Length + 1);
                                foreach (char c in prop.Value.StringValue)
                                    tempMem.WriteByte((byte)c);
                                tempMem.WriteByte(0);
                                break;
                            case SaltPropertyReader.Type.StructProperty:
                                tempMem.WriteValueS64(pcc.AddName(prop.Value.StringValue));
                                foreach (SaltPropertyReader.PropertyValue value in prop.Value.Array)
                                    tempMem.WriteValueS32(value.IntValue);
                                break;
                            case SaltPropertyReader.Type.ByteProperty:
                                tempMem.WriteValueS32(pcc.AddName(prop.Value.StringValue));
                                tempMem.WriteValueS32(prop.Value.IntValue);
                                break;
                            case SaltPropertyReader.Type.FloatProperty:
                                tempMem.WriteValueF32(prop.Value.FloatValue, Endian.Little);
                                break;
                            default:
                                throw new FormatException("unknown property");
                        }
                    }
                }
                buff = tempMem.ToArray();
            }

            int propertiesOffset = SaltPropertyReader.detectStart(pcc, buff);
            headerData = new byte[propertiesOffset];
            Buffer.BlockCopy(buff, 0, headerData, 0, propertiesOffset);
            properties = new Dictionary<string, SaltPropertyReader.Property>();
            List<SaltPropertyReader.Property> tempProperties = SaltPropertyReader.getPropList(pcc, buff);
            UnpackNum = 0;
            for (int i = 0; i < tempProperties.Count; i++)
            {
                SaltPropertyReader.Property property = tempProperties[i];
                if (property.Name == "UnpackMin")
                    UnpackNum++;

                if (!properties.ContainsKey(property.Name))
                    properties.Add(property.Name, property);

                switch (property.Name)
                {
                    case "Format": texFormat = property.Value.StringValue; break;
                    case "LODGroup": LODGroup = property.Value.StringValue; break;
                    case "CompressionSettings": Compression = property.Value.StringValue; break;
                    case "None": dataOffset = (uint)(property.offsetval + property.Size); break;
                }
            }

            // if "None" property isn't found throws an exception
            if (dataOffset == 0)
                throw new Exception("\"None\" property not found");
        }

        /// <summary>
        /// This function will first guess and then do a thorough search to find the original location of the texture
        /// </summary>
        private string FindFile()
        {
            if (!String.IsNullOrEmpty(oriPackage))
                return oriPackage;

            // KFreon:  All files should have been added elsewhere rather than searched for here
            if (allFiles == null)
            {
                allFiles = new List<string>(ME1Directory.Files);
                

            }
            string package = FullPackage.Split('.')[0];
            for (int i = 0; i < allFiles.Count; i++)
            {
                string[] parts = allFiles[i].Split('\\');
                string tempFile = parts.Last().Split('.')[0];
                if (String.Compare(package, tempFile, true) == 0)
                    return allFiles[i];
            }
            /*if (!KFreonLib.Misc.Methods.DisplayYesNoDialogBox("Package guessing failed. Would you like to do the thorough check? (LONG)", "Continue?"))
                return null;*/
            for (int i = 0; i < allFiles.Count; i++)
            {
                ME1PCCObject temp = new ME1PCCObject(allFiles[i]);
                for (int j = 0; j < temp.ExportCount; j++)
                {
                    ME1ExportEntry exp = temp.Exports[j];
                    if (String.Compare(texName, exp.ObjectName, true) == 0 && exp.ClassName == "ME1Texture2D")
                    {
                        ME1Texture2D temptex = new ME1Texture2D(temp, j);
                        if (temptex.privateimgList[0].storageType == storage.pccCpr || temptex.privateimgList[0].storageType == storage.pccSto)
                        {
                            return allFiles[i];
                        }
                    }
                }
            }
            return null;
        }

        public byte[] DumpImage(ImageSize imgSize)
        {
            byte[] imgBuffer = null;

            ImageInfo imgInfo;
            if (privateimgList.Exists(img => (img.imgSize == imgSize && img.cprSize != -1)))
                imgInfo = privateimgList.Find(img => img.imgSize == imgSize);
            else
                //throw new FileNotFoundException("Image with resolution " + imgSize + " not found");
                return null;
            switch (imgInfo.storageType)
            {
                case storage.pccSto:
                    imgBuffer = new byte[imgInfo.uncSize];
                    Buffer.BlockCopy(imageData, imgInfo.offset, imgBuffer, 0, imgInfo.uncSize);
                    break;
                case storage.arcCpr:
                case storage.arcUnc:
                    string archivePath = FindFile();
                    if (String.IsNullOrEmpty(archivePath))
                        throw new FileNotFoundException();
                    ME1PCCObject temp = new ME1PCCObject(archivePath);
                    for (int i = 0; i < temp.ExportCount; i++)
                    {
                        if (String.Compare(texName, temp.Exports[i].ObjectName, true) == 0 && (temp.Exports[i].ClassName == "Texture2D"))// || temp.Exports[i].ClassName == "TextureFlipBook"))
                        {
                            ME1Texture2D temptex = new ME1Texture2D(temp, i);
                            /*if (imgSize.width > dims)
                            {
                                dims = (int) imgSize.width;*/
                            byte[] temp1 = temptex.DumpImage(imgSize);
                            if (temp1 != null)
                                imgBuffer = temp1;
                            //}

                        }
                    }
                    break;
                case storage.pccCpr:
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        SaltLZOHelper lzohelp = new SaltLZOHelper();
                        imgBuffer = lzohelp.DecompressTex(ms, imgInfo.offset, imgInfo.uncSize, imgInfo.cprSize);
                    }
                    break;
                default:
                    throw new FormatException("Unsupported texture storage type");
                    imgBuffer = null;
                    break;
            }

            return imgBuffer;
        }

        public DDSFormat GetDDSFormat()
        {
            switch (texFormat)
            {
                case "PF_DXT1":
                    return DDSFormat.DXT1;
                case "PF_DXT5":
                    return DDSFormat.DXT5;
                case "PF_NormalMap_HQ":
                    return DDSFormat.ATI2;
                default:
                    throw new FormatException("Unknown or non-DDS Format");
            }
        }

        private void ChangeFormat(string newformat)
        {
            if (newformat == "PF_R8G8B8" || newformat == "R8G8B8")
                throw new FormatException("24-bit textures are not allowed in ME1");
            if (texFormat != "PF_NormalMap_HQ")
            {
                if (texFormat != newformat && texFormat != "PF_" + newformat)
                {
                    if (newformat.Substring(0, 3) != "PF_")
                        texFormat = "PF_" + newformat;
                    else
                        texFormat = newformat;
                    properties["Format"].Value.StringValue = texFormat;
                }
            }
            else
            {
                if (newformat != "ATI2")
                {
                    if (newformat.Substring(0, 3) != "PF_")
                        texFormat = "PF_" + newformat;
                    else
                        texFormat = newformat;
                    properties["Format"].Value.StringValue = texFormat;
                }
            }
        }

        public void LowResFix(int MipMapsToKeep = 1)
        {
            while (privateimgList[0].storageType == storage.empty)
            {
                numMipMaps--;
                privateimgList.RemoveAt(0);
            }

            while (privateimgList.Count > MipMapsToKeep)
            {
                numMipMaps--;
                privateimgList.Remove(privateimgList.Last());
            }

            numMipMaps = (uint)MipMapsToKeep;
            if (properties.ContainsKey("MipTailBaseIdx"))
                properties["MipTailBaseIdx"].Value.IntValue = 0;
            if (properties.ContainsKey("SizeX"))
                properties["SizeX"].Value.IntValue = (int)privateimgList[0].imgSize.width;
            if (properties.ContainsKey("SizeY"))
                properties["SizeY"].Value.IntValue = (int)privateimgList[0].imgSize.height;
        }

        // New methods!
        public void OneSizeFitsAll(byte[] data, bool resFix = false)
        {
            // forced lowresfix
            // resFix = true;

            if (data == null)
                throw new FileNotFoundException("Input data missing or inaccessible.");

            bool containsmips = true;
            ImageMipMapHandler mipmaps = null;

            if (privateimgList.Count > 1)
            {
                try { mipmaps = new ImageMipMapHandler("", data); }
                catch (FormatException) { containsmips = false; }
            }
            else
                containsmips = false;

            ImageInfo existingImg = privateimgList.First(img => img.storageType != storage.empty);
            if (containsmips)
            {
                if ((float)mipmaps.imageList[0].imgSize.width / (float)mipmaps.imageList[0].imgSize.height != (float)existingImg.imgSize.width / (float)existingImg.imgSize.height)
                    throw new FormatException("Input texture not correct aspect ratio");

                if (mipmaps.imageList[0].format == "PF_R8G8B8") // Convert to 32-bit if necessary
                {
                    for (int i = 0; i < mipmaps.imageList.Count; i++)
                        mipmaps.imageList[i] = new DDS(null, mipmaps.imageList[i].imgSize, "A8R8G8B8", ImageMipMapHandler.ConvertTo32bit(mipmaps.imageList[i].resize(), (int)mipmaps.imageList[i].imgSize.width, (int)mipmaps.imageList[i].imgSize.height));
                }

                if (Class == class2 || Class == class3) // Allow format modification if one of the derived classes. Don't need the single level check since we're replacing all levels
                    ChangeFormat(mipmaps.imageList[0].format);

                if (texFormat == "PF_NormalMap_HQ") // Check formats
                {
                    if (mipmaps.imageList[0].format != "ATI2")
                        throw new FormatException("Texture not in correct format - Expected ATI2");
                }
                else if (String.Compare(texFormat, "PF_" + mipmaps.imageList[0].format, true) != 0)
                    throw new FormatException("Texture not in correct format - Expected " + texFormat);

                for (int i = mipmaps.imageList.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (privateimgList.Exists(img => img.imgSize == mipmaps.imageList[i].imgSize))
                            ReplaceImage(mipmaps.imageList[i]);
                        else if (mipmaps.imageList[i].imgSize.width > privateimgList.First().imgSize.width && mipmaps.imageList[i].imgSize.height > privateimgList.First().imgSize.height)
                            UpscaleImage(mipmaps.imageList[i]);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("ERROR: " + e.Message);
                        return;
                    }

                    //else
                    //    AddMissingImage(mipmaps.imageList[i]);
                    // Else ignore missing values
                }

                while (privateimgList[0].imgSize.width > mipmaps.imageList[0].imgSize.width) // Remove any existing higher levels
                    privateimgList.RemoveAt(0);
            }
            else
            {
                ImageFile ddsfile = new DDS("", data);

                if ((float)ddsfile.imgSize.width / (float)ddsfile.imgSize.height != (float)existingImg.imgSize.width / (float)existingImg.imgSize.height) // Check dimensions
                    throw new FormatException("Input texture not correct aspect ratio");

                if (ddsfile.format == "R8G8B8")
                    ddsfile = new DDS(null, ddsfile.imgSize, "A8R8G8B8", ImageMipMapHandler.ConvertTo32bit(ddsfile.resize(), (int)ddsfile.imgSize.width, (int)ddsfile.imgSize.height));

                if (privateimgList.Count == 1 && (Class == class2 || Class == class3)) // Since this is single level replacement, only allow format change if a single level texture with required class
                    ChangeFormat(ddsfile.format);

                if (texFormat == "PF_NormalMap_HQ") // Check format
                {
                    if (ddsfile.format != "ATI2")
                        throw new FormatException("Texture not in correct format - Expected ATI2");
                }
                else if (String.Compare(texFormat, "PF_" + ddsfile.format, true) != 0)
                    throw new FormatException("Texture not in correct format - Expected " + texFormat);

                if (privateimgList.Count == 1 && privateimgList[0].imgSize != ddsfile.imgSize) // If img doesn't exist and it's a single level texture, use hard replace
                    HardReplaceImage(ddsfile);
                else if (privateimgList.Exists(img => img.imgSize == ddsfile.imgSize)) // Catches the rest of the single levels and every one which has an existing reference for that level
                    ReplaceImage(ddsfile);
                else if (ddsfile.imgSize.width > privateimgList[0].imgSize.width) // Add a greater image
                    UpscaleImage(ddsfile);
                //else if (ddsfile.imgSize.width < imgList.Last().imgSize.width) // Add a smaller image
                //    AddMissingImage(ddsfile);
            }

            if (privateimgList.Count > 1 && resFix)
                LowResFix();

            // Fix up properties
            if (properties.ContainsKey("SizeX"))
                properties["SizeX"].Value.IntValue = (int)privateimgList.First(img => img.storageType != storage.empty).imgSize.width;
            if (properties.ContainsKey("SizeY"))
                properties["SizeY"].Value.IntValue = (int)privateimgList.First(img => img.storageType != storage.empty).imgSize.height;
            if (properties.ContainsKey("MipTailBaseIdx"))
                properties["MipTailBaseIdx"].Value.IntValue = privateimgList.Count - 1;
            numMipMaps = (uint)privateimgList.Count;
        }

        public void HardReplaceImage(ImageFile ddsfile)
        {
            ImageSize imgSize = ddsfile.imgSize;

            if (ddsfile.format == "R8G8B8")
            {
                byte[] buff = ImageMipMapHandler.ConvertTo32bit(ddsfile.imgData, (int)ddsfile.imgSize.width, (int)ddsfile.imgSize.height);
                ddsfile = new DDS(null, ddsfile.imgSize, "A8R8G8B8", buff);
            }

            ImageInfo newImg = new ImageInfo();
            newImg.storageType = privateimgList[0].storageType;
            if (newImg.storageType == storage.empty || newImg.storageType == storage.arcCpr || newImg.storageType == storage.arcUnc)
                throw new FormatException("Original texture cannot be empty or externally stored");

            newImg.offset = 0;
            newImg.imgSize = imgSize;
            imageData = ddsfile.resize();
            newImg.uncSize = imageData.Length;
            if ((long)newImg.uncSize != ImageFile.ImageDataSize(imgSize, ddsfile.format, ddsfile.BPP))
                throw new FormatException("Input texture not correct length!");

            switch (newImg.storageType)
            {
                case storage.pccSto:
                    newImg.cprSize = imageData.Length;
                    break;
                case storage.pccCpr:
                    SaltLZOHelper lzohelper = new SaltLZOHelper();
                    imageData = lzohelper.CompressTex(imageData);
                    newImg.cprSize = imageData.Length;
                    break;
            }

            privateimgList.RemoveAt(0);
            privateimgList.Add(newImg);

            // Fix up properties
            properties["SizeX"].Value.IntValue = (int)imgSize.width;
            properties["SizeY"].Value.IntValue = (int)imgSize.height;
        }

        public void ReplaceImage(ImageFile ddsfile)
        {
            ImageSize imgSize = ddsfile.imgSize;

            int imageIdx = privateimgList.FindIndex(img => img.imgSize == imgSize);
            ImageInfo imgInfo = privateimgList[imageIdx];

            if (imgInfo.storageType == storage.empty && imgInfo.imgSize.width > privateimgList.First(img => img.storageType != storage.empty).imgSize.width)
                imgInfo.storageType = privateimgList.First(img => img.storageType != storage.empty).storageType;
            else if (imgInfo.storageType == storage.empty)
                imgInfo.storageType = privateimgList.Last(img => img.storageType != storage.empty).storageType;
            if (imgInfo.storageType == storage.arcCpr || imgInfo.storageType == storage.arcUnc)
                throw new FormatException("Replacement of externally stored textures is not allowed");
            else if (imgInfo.storageType == storage.empty)
                throw new FormatException("Cannot replace images with empty image lists");

            byte[] imgBuff = ddsfile.resize();

            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteBytes(imageData);
                imgInfo.uncSize = imgBuff.Length;
                if ((long)imgInfo.uncSize != ImageFile.ImageDataSize(ddsfile.imgSize, ddsfile.format, ddsfile.BPP))
                    throw new FormatException("Input texture not correct length!");

                if (imgInfo.storageType == storage.pccCpr)
                {
                    SaltLZOHelper lzo = new SaltLZOHelper();
                    imgBuff = lzo.CompressTex(imgBuff);
                }
                if (imgBuff.Length <= imgInfo.cprSize && imgInfo.offset > 0)
                    ms.Seek(imgInfo.offset, SeekOrigin.Begin);
                else
                    imgInfo.offset = (int)ms.Position;
                imgInfo.cprSize = imgBuff.Length;
                ms.WriteBytes(imgBuff);
                imageData = ms.ToArray();
            }
            privateimgList[imageIdx] = imgInfo;
        }

        public void UpscaleImage(ImageFile ddsfile)
        {
            ImageSize newImgSize = ddsfile.imgSize;
            ImageSize topImgSize = privateimgList.First().imgSize;
            List<ImageInfo> newImgs = new List<ImageInfo>();

            // First of all, add in all missing values in between
            ImageInfo newImg = new ImageInfo();
            newImg.cprSize = 0;
            newImg.uncSize = 0;
            newImg.storageType = storage.empty;
            newImg.offset = -1;
            newImg.imgSize = newImgSize;
            newImgs.Add(newImg);

            while ((topImgSize.width * 2) != newImgs.Last().imgSize.width && (topImgSize.height * 2) != newImgs.Last().imgSize.height)
            {
                newImg = new ImageInfo();
                newImg.cprSize = 0;
                newImg.uncSize = 0;
                newImg.storageType = storage.empty;
                newImg.offset = -1;
                newImg.imgSize = new ImageSize(newImgs.Last().imgSize.width / 2, newImgs.Last().imgSize.height / 2);
                // ^Slightly naive solution as it will fail on compressed textures smaller than 4x4, but since this is an upscale mechanism this shouldn't happen
                newImgs.Add(newImg);
            }

            privateimgList.InsertRange(0, newImgs); // Insert the new list
            ReplaceImage(ddsfile); // And regular replace
        }

        public void AddMissingImage(ImageFile ddsfile)
        {
            ImageInfo newImg = new ImageInfo();
            newImg.storageType = storage.empty;
            newImg.imgSize = ddsfile.imgSize;
            newImg.cprSize = 0;
            newImg.uncSize = 0;
            newImg.offset = -1;

            ImageInfo lastImg = privateimgList.Last();
            if (newImg.imgSize.width < lastImg.imgSize.width && newImg.imgSize.height < lastImg.imgSize.height) // Simple solution. Should normally be necessary
            {
                privateimgList.Add(newImg);
                ReplaceImage(ddsfile);
                return;
            }

            for (int i = 0; i < privateimgList.Count; i++) // Catch solution
            {
                if (newImg.imgSize.width < privateimgList[i].imgSize.width)
                    continue;
                privateimgList.Insert(i, newImg);
                ReplaceImage(ddsfile);
                return;
            }
            throw new Exception("Couldn't add missing image!"); // Safety catch
        }

        private List<ImageInfo> privateimgList { get; set; } // showable image list
        public List<IImageInfo> imgList
        {
            get
            {
                List<IImageInfo> retval = new List<IImageInfo>();
                foreach (ImageInfo inf in privateimgList)
                    retval.Add(inf);
                return retval;
            }
            set
            {
                List<ImageInfo> retval = new List<ImageInfo>();
                foreach (IImageInfo inf in value)
                    retval.Add((ImageInfo)inf);
                privateimgList = retval;
            }
        }


        public string texName
        {
            get;
            set;
        }

        public byte[] DumpImg(ImageSize imageSize, string ArcPath)
        {
            return DumpImage(imageSize);
        }

        public string texFormat
        {
            get;
            set;
        }

        public List<string> allPccs
        {
            get;
            set;
        }

        public uint pccOffset
        {
            get;
            set;
        }

        public bool hasChanged
        {
            get;
            set;
        }

        public string GetTexArchive(string dir)
        {
            throw new NotImplementedException();
        }

        public List<int> expIDs
        {
            get;
            set;
        }

        public void addBiggerImage(ImageFile im, string archiveDir)
        {
            addBiggerImage(im);
        }

        public void singleImageUpscale(ImageFile im, string archiveDir)
        {
            ImageSize biggerImageSizeOnList = privateimgList.Max(image => image.imgSize);
            // check if replacing image is supported
            ImageFile imgFile = im;

            if (!Methods.CheckTextureFormat(texFormat, imgFile.format))
                throw new FormatException("Different image format, original is " + texFormat + ", new is " + imgFile.subtype());

            // !!! warning, this method breaks consistency between imgList and imageData[] !!!
            ImageInfo newImgInfo = new ImageInfo();
            newImgInfo.storageType = privateimgList.Find(img => img.storageType != storage.empty).storageType;
            newImgInfo.imgSize = imgFile.imgSize;
            newImgInfo.uncSize = imgFile.resize().Length;
            newImgInfo.cprSize = 0x00; // not yet filled
            newImgInfo.offset = 0x00; // not yet filled
            privateimgList.RemoveAt(0);  // Remove old single image and add new one
            privateimgList.Add(newImgInfo);

            //now I let believe the program that I'm doing an image replace, saving lot of code ;)
            replaceImage(newImgInfo.imgSize.ToString(), im, false, archiveDir);

            // update Sizes
            properties["SizeX"].Value.IntValue = (int)newImgInfo.imgSize.width;
            properties["SizeY"].Value.IntValue = (int)newImgInfo.imgSize.height;
        }

        public void OneImageToRuleThemAll(ImageFile im, string archiveDir, byte[] imgData)
        {
            OneSizeFitsAll(imgData);
            //OneImageToRuleThemAll(imageFilename);
        }

        public string arcName
        {
            get
            {
                return ""; // Heff: No .tfc's present in ME1, returning this to hopefully not have to deal with the ME1 exception in all places.
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private string privateLODGroup = "None";
        public string LODGroup
        {
            get
            {
                return privateLODGroup;
            }
            set
            {
                privateLODGroup = value;
            }
        }

        public uint Hash
        {
            get;
            set;
        }

        public void removeImage()
        {
            throw new NotImplementedException();
        }

        public byte[] ToArray(uint pccExportDataOffset, IPCCObject pcc)
        {
            return ToArray((int)pccExportDataOffset, (ME1PCCObject)pcc);
        }

        public int pccExpIdx
        {
            get;
            set;
        }

        public void CopyImgList(ITexture2D tex2D, IPCCObject PCC)
        {
            CopyImgList((ME1Texture2D)tex2D, (ME1PCCObject)PCC);
        }


        public byte[] extractMaxImage(bool NoOutput, string archiveDir = null, string fileName = null)
        {
            // select max image size, excluding void images with offset = -1
            ImageSize maxImgSize = privateimgList.Where(img => img.offset != -1).Max(image => image.imgSize);
            // extracting max image
            return extractImage(privateimgList.Find(img => img.imgSize == maxImgSize), NoOutput, archiveDir, fileName);
        }


        public int Mips
        {
            get;
            set;
        }


        public bool NoRenderFix
        {
            get;
            set;
        }


        public void LowResFix()
        {
            LowResFix(1);
        }

        public IImageInfo GenerateImageInfo()
        {
            IImageInfo imginfo = privateimgList.First(img => (int)img.storageType != (int)ME1Texture2D.storage.empty);
            imginfo.GameVersion = 1;
            return imginfo;
        }

        public System.Drawing.Bitmap GetImage(int size = -1)
        {
            try
            {
                byte[] imgdata = GetImageData(size);
                if (imgdata == null)
                    return null;
                using (ImageEngineImage img = new ImageEngineImage(imgdata))
                    return img.GetGDIBitmap(false);
            }
            catch { }
            return null;
        }

        public byte[] GetImageData(int size)
        {
            byte[] imgdata = null;
            if (size == -1)
                imgdata = extractMaxImage(true);
            else
            {
                ImageSize tes;
                if (privateimgList.Count != 1)
                    tes = privateimgList.Where(img => (img.imgSize.width <= size || img.imgSize.height <= size) && img.offset != -1).Max(image => image.imgSize);
                else
                    tes = privateimgList.First().imgSize;
                imgdata = extractImage(tes.ToString(), true);
            }
            return imgdata;
        }

        public void DumpTexture(string filename)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                this.allFiles = null;
                this.allPccs = null;
                this.expIDs = null;
                this.footerData = null;
                this.headerData = null;
                this.imageData = null;
                this.privateimgList = null;

                disposedValue = true;
            }
        }

         ~ME1Texture2D()
        {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
         }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
             GC.SuppressFinalize(this);
        }
        #endregion
    }
}