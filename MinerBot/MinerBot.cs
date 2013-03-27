﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EveCom;

namespace MinerBot
{
    public partial class MinerBot : Form
    {
        public MinerBot()
        {
            InitializeComponent();
        }

        Bot bot;
        
        private void Form1_Load(object sender, EventArgs e)
        {
            using (new EVEFrameLock())
            {
            }
            bot = new Bot();
            txtStation.Text = Properties.Settings.Default.Station;
        }

        private void txtStation_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Station = txtStation.Text;
            Properties.Settings.Default.Save();
        }

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (chkActive.Checked)
            {
                bot.QueueState(bot.InBelt);
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
                txtStation.Text = Session.StationName;
                Properties.Settings.Default.Station = txtStation.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (bot.CurState != null)
            {
                lblState.Text = "State: " + bot.CurState.ToString();
            }
            else
            {
                lblState.Text = "State: Idle";
            }
            lblCurRoidOre.Text = "CurRoid Ore: " + bot.CurRoidOre.ToString("F2") + " m^3";
            lblEstimatedMined.Text = "EstimatedMined: " + bot.EstimatedMined.ToString("F2") + " m^3";
        }
    }
}