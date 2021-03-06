﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FlowCtlBaseModel;
using AsrsControl;
namespace PrcsCtlModelsAoyou
{
    public class NodeSwitchInput : CtlNodeBaseModel
    {
        public delegate string DlgtGetAsrsLogicArea(string palletID, AsrsCtlModel asrsCtl, int curStep);
        private short barcodeFailedStat = 1;
        public DlgtGetAsrsLogicArea dlgtGetLogicArea = null;
        private List<FlowPathModel> flowPathList = new List<FlowPathModel>();
        public AsrsInterface.IAsrsManageToCtl AsrsResManage { get; set; }
        /// <summary>
        /// 建立路径列表，只建两级路径，分流点-入口-堆垛机
        /// </summary>
        public override void BuildPathList()
        {
            int pathSeq = 1;
            foreach(CtlNodeBaseModel node in NextNodes)
            {
                foreach(CtlNodeBaseModel nextNode in node.NextNodes)
                {
                     FlowPathModel path = new FlowPathModel();
                     path.PathSeq = pathSeq;
                     path.AddNode(node);
                     path.AddNode(nextNode);
                     flowPathList.Add(path);
                     pathSeq++;
                }
            }
        }
        public override bool ExeBusiness(ref string reStr)
        {
            if (!devStatusRestore)
            {
                devStatusRestore = DevStatusRestore();
            }
            if (db2Vals[0] == 1)
            {
             
                currentTaskPhase = 0;
                Array.Clear(this.db1ValsToSnd, 0, this.db1ValsToSnd.Count());
                rfidUID = string.Empty;
                currentTaskDescribe = "等待新的任务";
                //return true;
            }
            //if(db1ValsToSnd[0] >1) //分流完成后
            //{
            //    return true;
            //}
            if (db2Vals[0] == 2)
            {
                if (currentTaskPhase == 0)
                {
                    currentTaskPhase = 1;
                }
            }
            switch(this.currentTaskPhase)
            {
                case 1:
                    {
                        currentTaskDescribe = "开始读RFID";
                        this.rfidUID = "";
                        if (SysCfg.SysCfgModel.UnbindMode)
                        {
                            this.rfidUID = System.Guid.NewGuid().ToString();
                        }
                        else
                        {
                            if (SysCfg.SysCfgModel.SimMode || SysCfg.SysCfgModel.RfidSimMode)
                            {
                                this.rfidUID = this.SimRfidUID;
                            }
                            else
                            {
                                this.rfidUID = this.barcodeRW.ReadBarcode();
                               
                            }
                        }
                       
                        if (string.IsNullOrWhiteSpace(this.rfidUID))
                        {
                            if (this.db1ValsToSnd[0] != barcodeFailedStat)
                            {
                                logRecorder.AddDebugLog(nodeName, "读料框条码失败");
                            }
                            this.db1ValsToSnd[0] = barcodeFailedStat;
                            break;
                        }
                        this.rfidUID = this.rfidUID.Trim(new char[] { '\0', '\r', '\n', '\t', ' ' });
                       
                        string palletPattern = @"^[a-z|A-Z|0-9]{4}TP[0-9]{4,}";
                        if(!Regex.IsMatch(this.rfidUID,palletPattern))
                        {
                            if (this.db1ValsToSnd[0] != barcodeFailedStat)
                            {
                                logRecorder.AddDebugLog(nodeName, "读料框条码不符合规则，" + this.rfidUID);
                                this.currentTaskDescribe = "读料框条码不符合规则，" + this.rfidUID;
                            }
                            this.db1ValsToSnd[0] = barcodeFailedStat;
                            break;
                        }
                        /*
                        //检测是否跟库里有重码
                        string[] houseNames = new string[] { AsrsModel.EnumStoreHouse.A1库房.ToString(), AsrsModel.EnumStoreHouse.A2库房.ToString(),
                            AsrsModel.EnumStoreHouse.B1库房.ToString(), AsrsModel.EnumStoreHouse.C1库房.ToString(),AsrsModel.EnumStoreHouse.C2库房.ToString(),AsrsModel.EnumStoreHouse.C3库房.ToString() };
                        foreach (string houseName in houseNames)
                        {
                            string cellIn = AsrsResManage.IsProductCodeInStore(houseName, this.rfidUID, ref reStr);
                            if (!string.IsNullOrWhiteSpace(cellIn))
                            {
                                if (this.db1ValsToSnd[0] != 3)
                                {
                                    currentTaskDescribe = string.Format("条码异常，条码{0}已经在库房{1},库位{2}", this.rfidUID.Length.ToString(), houseName, cellIn);
                                    logRecorder.AddDebugLog(nodeName, currentTaskDescribe);
                                }
                                this.db1ValsToSnd[0] = 3;
                                return true;
                            }
                        }*/

                        logRecorder.AddDebugLog(this.nodeName, "读到托盘号:" + this.rfidUID);
                        this.currentTaskPhase++;
                        break;
                    }
                case 2:
                    {
                        //分流
                        currentTaskDescribe = "等待分流";
                        int switchRe = 0;
                        int step = 0;
                        if (!MesAcc.GetStep(this.rfidUID, out step, ref reStr))
                        {
                            currentTaskDescribe = "查询MES工步失败:" + reStr;
                            break;
                        }
                        if(this.nodeID=="4001")
                        {
                            if(step==0)
                            {
                                currentTaskDescribe = string.Format("{0} 入库分流失败，步号为0，禁止入库", this.rfidUID);
                                if(this.db1ValsToSnd[0] != 4)
                                {
                                    logRecorder.AddDebugLog(nodeName, string.Format("{0} 入库分流失败，步号为0，禁止入库", this.rfidUID));

                                }
                                this.db1ValsToSnd[0] = 4; //

                                break;
                            }
                        }
                        if(this.nodeID=="4004")
                        {
                            step = 5;
                            if (!MesAcc.UpdateStep(step, this.rfidUID, ref reStr))
                            {
                                currentTaskDescribe = "更新MES工步失败:" + reStr;
                                break;
                            }
                        }
                        if (this.nodeID == "4005")
                        {
                            step = 6;
                            if (!MesAcc.UpdateStep(step, this.rfidUID, ref reStr))
                            {
                                currentTaskDescribe = "更新MES工步失败:" + reStr;
                                break;
                            }
                        }
                        if(this.nodeID=="4006")
                        {
                            if(step>0)
                            {
                                if (step >= 11)
                                {
                                    step = 0;
                                    if (!MesAcc.UpdateStep(step, this.rfidUID, ref reStr))
                                    {
                                        currentTaskDescribe = "更新MES工步失败:" + reStr;
                                        break;
                                    }
                                }
                                else if (step !=10)
                                {
                                    step = 10;
                                    if (!MesAcc.UpdateStep(step, this.rfidUID, ref reStr))
                                    {
                                        currentTaskDescribe = "更新MES工步失败:" + reStr;
                                        break;
                                    }
                                }
                                
                            }
                           
                        }
                        FlowPathModel switchPath = FindFirstValidPath(this.rfidUID, ref reStr);
                        if(switchPath == null)
                        {
                            switchRe = 0; //无可用路径，等待
                            this.db1ValsToSnd[0] = (short)switchRe;
                            this.currentTaskDescribe = reStr;
                            break;
                        }
                        else
                        {
                            
                            CtlNodeBaseModel node = switchPath.NodeList[0];
                           
                            if(this.nodeID== "4004")
                            {
                                AsrsControl.AsrsPortalModel port = (node as AsrsControl.AsrsPortalModel);
                                if(port == null)
                                {
                                    break;
                                }
                                AsrsModel.CellCoordModel requireCell = null;
                                AsrsControl.AsrsCtlModel asrsCtl = port.AsrsCtl;
                                AsrsInterface.IAsrsManageToCtl asrsResManage = port.AsrsCtl.AsrsResManage;
                                string logicArea = "通用分区";
                                if(dlgtGetLogicArea != null)
                                {
                                    logicArea = dlgtGetLogicArea(this.rfidUID, asrsCtl, step);
                                }
                                if(string.IsNullOrWhiteSpace(logicArea))
                                {
                                    this.currentTaskDescribe = string.Format("{0} 检索货区失败,未配置", this.rfidUID);
                                    break;
                                }
                               // AsrsModel.EnumLogicArea logicArea = (AsrsModel.EnumLogicArea)Enum.Parse(typeof(AsrsModel.EnumLogicArea), SysCfg.SysCfgModel.asrsStepCfg.AsrsAreaSwitch(step)); 
                                if (!asrsResManage.CellRequire(asrsCtl.HouseName,logicArea, ref requireCell, ref reStr))
                                {
                                    Console.WriteLine("{0}申请库位失败,{1}",nodeName, reStr);
                                    break;
                                }
                                if(requireCell.Row == 1)
                                {
                                    switchRe = 2;
                                    asrsCtl.SetAsrsCheckinRow(requireCell.Row);
                                }
                                else if (requireCell.Row == 2)
                                {
                                    switchRe = 3;
                                    asrsCtl.SetAsrsCheckinRow(requireCell.Row);
                                }
                                else
                                {
                                    break;
                                }
                                logRecorder.AddDebugLog(nodeName, string.Format("{0}锁定分容库位第{1}排",this.rfidUID,requireCell.Row));

                                
                            }
                            else
                            {
                                switchRe = switchPath.PathSeq + 1;
                            }
                           
                            this.db1ValsToSnd[0] = (short)switchRe;
                            /*
                            if(this.nodeID=="4006") //OCV后入库分流时，把条码后4位以整形发给PLC
                            {
                                this.db1ValsToSnd[1] = short.Parse(this.rfidUID.Substring(this.rfidUID.Length - 4, 4));
                            }*/
                            if (node.GetType().ToString() == "AsrsControl.AsrsPortalModel")
                            {
                                (node as AsrsControl.AsrsPortalModel).PushPalletID(this.rfidUID);

                            }
                            string logStr = string.Format("{0}分流，进入{1}", this.rfidUID, switchPath.NodeList[0].NodeName);
                            logRecorder.AddDebugLog(nodeName, logStr);
                            AddProduceRecord(this.rfidUID, logStr);
                           
                            if(this.nodeID=="4001")
                            {
                                if(step>=4)
                                {
                                    step = 0;
                                    if(!MesAcc.UpdateStep(step, this.rfidUID, ref reStr))
                                    {
                                        currentTaskDescribe = "更新MES工步失败:" + reStr;
                                        break;
                                    }
                                }
                            }
                        }
                       
                        this.currentTaskPhase++;
                        break;
                    }
                case 3:
                    {
                        currentTaskDescribe = "分流完成";
                        break;
                    }
                default:
                    break;
            }
            return true;
        }
        /// <summary>
        /// 搜索第一条可用路径
        /// </summary>
        /// <param name="palletID"></param>
        /// <param name="reStr"></param>
        /// <returns></returns>
        private FlowPathModel FindFirstValidPath(string palletID,ref string reStr)
        {
            List<FlowPathModel> validPathList = new List<FlowPathModel>();

            foreach (FlowPathModel path in flowPathList)
            {
                if (path.IsPathConnected(palletID, ref reStr))
                {
                    validPathList.Add(path);
                }
            }
            if(validPathList.Count()==0)
            {
                reStr = "没有可用分流路径，"+reStr;
                return null;
            }
            //排序
            FlowPathModel rePath = validPathList[0];
            if (validPathList.Count()>1)
            {
                CtlNodeBaseModel node1 = rePath.NodeList[0];
                int weight1 = node1.PathValidWeight(palletID, ref reStr);

                for(int i=1;i<validPathList.Count();i++)
                {
                    FlowPathModel path = validPathList[i];
                 
                    CtlNodeBaseModel node2 = path.NodeList[0];
                    int weight2 = node2.PathValidWeight(palletID, ref reStr);
                    if(weight2>weight1)
                    {
                        rePath = path;
                        weight1 = weight2;
                    }

                }
            }
            return rePath;
        }
        
       
    }
}
