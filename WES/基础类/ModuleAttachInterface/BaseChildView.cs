﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogInterface;
namespace ModuleCrossPnP
{
    public partial class BaseChildView : Form, IModuleAttach
    {
       // protected string captionText = "";
        protected ILogRecorder logRecorder = null;
        protected IParentModule parentPNP = null;
        protected MenuStrip menuStrip = null;
        //protected List<BaseChildView> childViews = new List<BaseChildView>();
        protected IDictionary<string, BaseChildView> childViewDic = new Dictionary<string, BaseChildView>();
        #region  公有接口
       // public string CaptionText { get { return captionText; } set { captionText = value; this.Text = captionText; } }
        public IDictionary<string, BaseChildView> ChildViewDic { get { return childViewDic; } }
        public BaseChildView()
        { }
        public BaseChildView(string captionText)
        {
            InitializeComponent();
            this.Text = captionText;
          //  this.captionText = captionText;
        }
        public bool RegistExtView(BaseChildView extView,ref string reStr)
        {
            try
            {
                if(extView==null)
                {
                    reStr = "空对象";
                    return false;
                }
                childViewDic[extView.Text] = extView;
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }
        }
        #endregion
        #region IModuleAttach接口实现
        public virtual void RegisterMenus(MenuStrip parentMenu, string rootMenuText)
        {
            this.menuStrip = parentMenu;
            ToolStripMenuItem rootMenuItem = new ToolStripMenuItem(rootMenuText);//parentMenu.Items.Add("仓储管理");
            rootMenuItem.Click += LoadMainform_MenuHandler;
            parentMenu.Items.Add(rootMenuItem);
        }
        public virtual void SetParent(/*Control parentContainer, Form parentForm, */IParentModule parentPnP)
        {
            this.parentPNP = parentPnP;
        }
        public virtual void SetLoginterface(ILogRecorder logRecorder)
        {
            this.logRecorder = logRecorder;
            
            //   lineMonitorPresenter.SetLogRecorder(logRecorder);
        }
        public virtual List<string> GetLogsrcList()
        {
            return null;
        }
        public virtual bool RoleEnabled()
        {
            return true;
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns>1：确定,其它：取消</returns>
        protected int PoupAskmes(string info)
        {
            if (DialogResult.OK == MessageBox.Show(info, "提示", MessageBoxButtons.OKCancel))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        private void LoadMainform_MenuHandler(object sender, EventArgs e)
        {
            this.parentPNP.AttachModuleView(this);
            
        }
        public  virtual void ChangeRoleID(int roleID)
        {

        }
    }
}
