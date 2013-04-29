using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EveCom;
using EveComFramework.Core;
using EveComFramework.Move;

namespace MinerBot
{
    public partial class MinerBot : Form
    {
        Bot bot = Bot.Instance;
        UIUpdate uiupdate = UIUpdate.Instance;
        MinerSettings Config = Bot.Instance.Config;

        public MinerBot()
        {
            InitializeComponent();
            Bot.Instance.Console.Event += Console;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bot.RegisterCommands();
            Dropoff.Text = Properties.Settings.Default.Station;
        }

        private void txtStation_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Station = Dropoff.Text;
            Properties.Settings.Default.Save();
        }

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (chkActive.Checked)
            {
                bot.QueueState(bot.InStation);
            }
            else
            {
                bot.Clear();
            }
        }

        private void btnCurrentStation_Click(object sender, EventArgs e)
        {
            using(new EVEFrameLock())
            {
                Dropoff.Text = Session.StationName;
                Properties.Settings.Default.Station = Dropoff.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                switch (comboBox1.SelectedItem.ToString())
                {
                    case "ItemHangar":
                        Config.DropoffType = DropoffType.ItemHangar;
                        break;
                    case "Jetcan":
                        Config.DropoffType = DropoffType.Jetcan;
                        break;

                }
            }
        }

        delegate void SetConsole(string Message);

        void Console(string Message)
        {
            if (listConsole.InvokeRequired)
            {
                listConsole.BeginInvoke(new SetConsole(Console), Message);
            }
            else
            {
                listConsole.Items.Add(Message);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dropoff.AutoCompleteCustomSource = new MyAutoCompleteStringCollection(uiupdate.Bookmarks); 
        }

    }

    public class MyAutoCompleteStringCollection : AutoCompleteStringCollection
    {
        public MyAutoCompleteStringCollection(List<String> items)
        {
            this.AddRange(items.ToArray());
        }
    }
}