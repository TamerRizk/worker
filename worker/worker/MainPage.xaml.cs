/*
 * Copyright (C) 2011 by Tamer Rizk, Inficron Inc., http://inficron.com
 * All rights reserved.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Browser;
using System.ComponentModel;
using System.Reflection;
using System.Security.Permissions;

// JS Engine: http://jurassic.codeplex.com
using Jurassic;
using Jurassic.Library;
using Jurassic.Compiler;

namespace worker
{
    public delegate void msg(string s);

    [ScriptableType]
    public partial class MainPage : UserControl
    {        
        [ScriptableMember]
        public string onworkercompleted { get; set; }
        [ScriptableMember]
        public string onworkermessage { get; set; }
        [ScriptableMember]
        public string timeout { get; set; }

        private BackgroundWorker bw = new BackgroundWorker(); 
        private string js { get; set; }
        private string value { get; set; }
        private string msg { get; set; }
        private bool completed { get; set; }

        public MainPage()
        {
            InitializeComponent();            
            this.onworkercompleted = "workercompleted";
            this.onworkermessage = "workermessage";
            this.timeout = "600";
            this.completed = false;
            this.value = "";
            this.msg = "";
            this.js = ""; 

            this.Loaded += new System.Windows.RoutedEventHandler(Page_Loaded);           
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);            
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);                        
        }

        void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Register this object in the page as Scriptable
            // so it can be access from within JavaScript
            HtmlPage.RegisterScriptableObject("sl", this);
        }

        [ScriptableMember]
        public void worker(string work)
        {            
            if (bw.IsBusy != true)
            {                
                bw.RunWorkerAsync(work);                
            }
        }

        private void wc_jsCompleted(object sender, DownloadStringCompletedEventArgs e)
        {            
            js = string.Empty;            
            if (e.Error == null)
            {
                js = e.Result;                
            }
        }
               
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string work = e.Argument as string;
            int tm = 31;

            System.Net.WebClient wc = new System.Net.WebClient();
            wc.Encoding = System.Text.Encoding.UTF8;
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_jsCompleted);
            try
            {
                wc.DownloadStringAsync(new Uri(work));
            }
            catch 
            {
                this.value = "Could not open script: " + work;
                tm = 0;
            }

            //js = "function onmessage(s){s='Child got: '+s;workermessage(s);}var i=1000000;var m=0;var o=[];while(i>0){i=i-1;m=Math.floor(Math.ceil(Math.random()*1000000)/(Math.ceil(Math.random()*1000)+1));if(m%19==0){o.push(m);}}workermessage(o.join(', '));workercompleted('done!');"; 
            
            while (0<--tm)
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    js = "";
                    break;
                }
                else if(js.Length>0)
                {
                    break;                    
                }
                Thread.Sleep(1000);
            }
            
            if (js.Length > 0)
            {
                var engine = new Jurassic.ScriptEngine();
                MethodInfo mi1 = typeof(MainPage).GetMethod("message", BindingFlags.Public | BindingFlags.Instance);
                Delegate del1 = Delegate.CreateDelegate(typeof(msg), this, mi1, true); ;
                MethodInfo mi2 = typeof(MainPage).GetMethod("complete", BindingFlags.Public | BindingFlags.Instance);
                Delegate del2 = Delegate.CreateDelegate(typeof(msg), this, mi2, true);
                engine.SetGlobalFunction(this.onworkermessage, del1);
                engine.SetGlobalFunction(this.onworkercompleted, del2);                
                engine.Execute(js);                

                tm = Int32.Parse(this.timeout);                
                while (0 < --tm)
                {
                    if(this.msg.Length>0)
                    {
                        //send a message to the worker
                        engine.CallGlobalFunction("onmessage", this.msg);
                        this.msg = "";
                    }
                    else if (worker.CancellationPending == true)
                    {
                        e.Cancel = true;
                        break;
                    }
                    else if (this.completed == true)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
        }
        [ScriptableMember]
        public void postmessage(string s)
        {
            this.msg = s;
        }

        [ScriptableMember]
        public void complete(string s)
        {
            this.value = s;
            this.completed = true;
        }

        [ScriptableMember]
        public void close()
        {
            this.bw.CancelAsync();            
        }

        [ScriptableMember]
        public void message(string s)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { HtmlPage.Window.Invoke(this.onworkermessage, s); });
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {   
            //Deployment.Current.Dispatcher.BeginInvoke(() => { HtmlPage.Window.Alert("end"); });            
            HtmlPage.Window.Invoke(this.onworkercompleted, this.value);
        }
    }

}