﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace QAScript
{
    public static class GeneralTests
    {
        public static void RunGeneralTests(Patient patient, Course course, PlanSetup plan)
        {
            //////////////////////////////////////////////
            // The main body of common code starts here //
            //////////////////////////////////////////////

            // Every new class needs to do these same first steps which is to load in the msg and the datatable from their propertes and write them back at the end of the code.
            string msg = SomeProperties.MsgString;
            DataTable table = SomeProperties.MsgDataTable;
            DataRow row;

            // Check primary ref point equals plan ID
            row = table.NewRow();
            row["Item"] = "The primary ref point equals the plan ID";
            if (plan.PrimaryReferencePoint.Id != plan.Id)
            {
                msg += "\n\nPrimary reference point \"" + plan.PrimaryReferencePoint.Id + "\" does not have the same name as the plan (\"" + plan.Id + "\").";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that the precribed isodose line is 100.
            row = table.NewRow();
            row["Item"] = "The precribed isodose line is 100";
            if (plan.TreatmentPercentage != 1)
            {
                msg += "\n\nThe prescribed percentage is not 100%. Please make sure this is intentional.";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check Beam isocenters are all the same
            row = table.NewRow();
            row["Item"] = "Beam isocenters are all the same";
            var xiso = plan.Beams.First().IsocenterPosition.x;
            var yiso = plan.Beams.First().IsocenterPosition.y;
            var ziso = plan.Beams.First().IsocenterPosition.z;
            var IsoNotEqual = 0;

            var listofbeams = plan.Beams;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsocenterPosition.x != xiso)
                {
                    IsoNotEqual = 1;
                }
                if (scan.IsocenterPosition.y != yiso)
                {
                    IsoNotEqual = 1;
                }
                if (scan.IsocenterPosition.z != ziso)
                {
                    IsoNotEqual = 1;
                }

            }
            if (IsoNotEqual == 1)
            {
                msg += "\n\nOne or more of the beams have different isocenters.";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that the machine is the same for all beams
            row = table.NewRow();
            row["Item"] = "The machine name is the same for all beams";
            // var Machine = plan.Beams.First().ExternalBeam.Id; //v11
            string Machine = plan.Beams.First().TreatmentUnit.Id; //v15
            bool MachineMatchIssue = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.TreatmentUnit.Id != Machine)
                {
                    MachineMatchIssue = true;
                }
            }
            if (MachineMatchIssue == true)
            {
                msg += "\n\nThe machine is not the same for all beams.";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that the jaw setting of each beam is at least 3 cm in x and y for all control points.
            row = table.NewRow();
            row["Item"] = "The jaw setting of each beam is at least 3 cm in x and y for all control points";
            bool foundsmallFS = false;
            foreach (Beam scan in listofbeams)
            {
                double SmallestFS = 400;
                var listofCP = scan.ControlPoints;
                foreach (ControlPoint cp in listofCP)
                {
                    double XFS;
                    double YFS;
                    double X1 = cp.JawPositions.X1;
                    double X2 = cp.JawPositions.X2;
                    double Y1 = cp.JawPositions.Y1;
                    double Y2 = cp.JawPositions.Y2;
                    GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                    if (XFS < SmallestFS)
                    {
                        SmallestFS = XFS;
                    }
                    if (YFS < SmallestFS)
                    {
                        SmallestFS = YFS;
                    }
                }
                if (SmallestFS < 30)
                {
                    msg += "\n\nField \"" + scan.Id + "\"contains an X or Y jaw setting smaller than 3 cm (at least one control point has a jaw width of " + SmallestFS / 10 + " cm).";
                    foundsmallFS = true;
                }
            }
            if (foundsmallFS == false)
            {
                row["Result"] = "Pass";
            }
            else
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check that the X Jaw setting is no greater than 20 cm for a beam of type "ARC" or "SRS ARC".
            row = table.NewRow();
            row["Item"] = "The X Jaw setting is no greater than 20 cm for a beam of type \"ARC\" or \"SRS ARC\"";
            bool FoundXFSTooBig = false;
            foreach (Beam scan in listofbeams)
            {
                bool FoundCPTooBig = false;
                double XFS = 0;
                double YFS = 0;
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    var listofCP = scan.ControlPoints;
                    foreach (ControlPoint cp in listofCP)
                    {
                        double X1 = cp.JawPositions.X1;
                        double X2 = cp.JawPositions.X2;
                        double Y1 = cp.JawPositions.Y1;
                        double Y2 = cp.JawPositions.Y2;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS > 200) //FS is in mm.
                        {
                            FoundCPTooBig = true;
                        }
                    }
                }

                if (FoundCPTooBig == true)
                {
                    msg += "\n\nField \"" + scan.Id.ToString() + "\" has an X jaw setting greater than 20 cm.";
                    FoundXFSTooBig = true;
                }
            }
            if (FoundXFSTooBig == false)
            {
                row["Result"] = "Pass";
            }
            else
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // For Fields of type "ARC" and "SRS ARC", check that collimator angle is not zero
            row = table.NewRow();
            row["Item"] = "For Fields of type \"ARC\" and \"SRS ARC\", check that collimator angle is not zero";
            bool BadColAngle = false;
            foreach (Beam scan in listofbeams)
            {
                bool BadColAngleinBeam = false;
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    var listofCP = scan.ControlPoints;
                    foreach (ControlPoint cp in listofCP)
                    {
                        if (cp.CollimatorAngle == 0)
                        {
                            BadColAngle = true;
                            BadColAngleinBeam = true;
                        }
                    }
                }
                if (BadColAngleinBeam == true)
                {
                    msg += "\n\nField \"" + scan.Id.ToString() + "\" is an arc and has a collimator setting of zero.";
                }
            }
            if (BadColAngle == false)
            {
                row["Result"] = "Pass";
            }
            else
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check that SU fields have a 15x15 cm2 jaw setting and CBCT fields have a 10x10 cm2 jaw setting
            row = table.NewRow();
            row["Item"] = "Check that setup fields have a 15x15 cm2 jaw setting and the CBCT field has a 10x10 cm2 jaw setting";
            bool foundbadSetupFS = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == true)
                {
                    if (scan.Id.ToLower().Contains("cbct"))
                    {
                        double X1 = scan.ControlPoints.First().JawPositions.X1;
                        double X2 = scan.ControlPoints.First().JawPositions.X2;
                        double Y1 = scan.ControlPoints.First().JawPositions.Y1;
                        double Y2 = scan.ControlPoints.First().JawPositions.Y2;
                        double XFS;
                        double YFS;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS != 100 || YFS != 100)
                        {
                            msg += "\n\nThe CBCT setup field does not have a jaw setting of 10x10 cm2.";
                            foundbadSetupFS = true;
                        }
                    }
                    if (!scan.Id.ToLower().Contains("cbct"))
                    {
                        double X1 = scan.ControlPoints.First().JawPositions.X1;
                        double X2 = scan.ControlPoints.First().JawPositions.X2;
                        double Y1 = scan.ControlPoints.First().JawPositions.Y1;
                        double Y2 = scan.ControlPoints.First().JawPositions.Y2;
                        double XFS;
                        double YFS;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS != 150 || YFS != 150)
                        {
                            msg += "\n\nThe setup field \"" + scan.Id + "\" does not have a jaw setting of 15x15 cm2.";
                            foundbadSetupFS = true;
                        }
                    }

                }
            }
            if (foundbadSetupFS == false)
            {
                row["Result"] = "Pass";
            }
            else
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check to make sure normalization is appplied
            row = table.NewRow();
            row["Item"] = "Check that some normalization is appplied to 3D plans and that RapidArc plans have the usual \"100% of the dose covers 95% of Target Volume\"";
            bool OddorNoNormalization = false;
            bool hasarc = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    hasarc = true;
                }
            }
            if (hasarc == true)
            {
                if (plan.PlanNormalizationMethod != "100.00% covers 95.00% of Target Volume")
                {
                    msg += "\n\nArc technique detected, but the normalization is not set to the usual \"100.00% covers 95.00% of Target Volume\". Was this intentional?";
                    OddorNoNormalization = true;
                }
            }
            if (hasarc == false)
            {
                if (plan.PlanNormalizationMethod == "No plan normalization")
                {
                    msg += "\n\nThe plan is not normalized.";
                    OddorNoNormalization = true;
                }
            }
            if (OddorNoNormalization == true)
            {
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check to make sure that RA beams alternate direction
            row = table.NewRow();
            row["Item"] = "Make sure that plans with arcs do not have all arcs sweep in the same direction as this would waste time at the delivery";
            int CW = 0;
            int CCW = 0;
            int DiffinCWvsCCW;
            foreach (Beam scan in listofbeams)
            {
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    if (scan.GantryDirection.ToString() == "Clockwise")
                    {
                        CW += 1;
                    }
                    if (scan.GantryDirection.ToString() == "CounterClockwise")
                    {
                        CCW += 1;
                    }
                }
            }
            DiffinCWvsCCW = CW - CCW;
            if (DiffinCWvsCCW < -1.5 || DiffinCWvsCCW > 1.5)
            {
                msg += "\n\nThe difference in the number of clockwise arcs compared to counterclockwise arcs is high. The treatment time may not be optimal.";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that SU images have the correct gantry angle based on field name and patient orientation.
            row = table.NewRow();
            row["Item"] = "Check that SU images have the correct gantry angle based on field name and patient orientation. It checks for \"ANT\", \"POST\", \"LT\", \"RT\" and \"CBCT\" then looks for the correct gantry angle. It also checks that the collimator angle is zero.";
            bool foundbadSUgantry = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == true)
                {
                    if (scan.ControlPoints.First().CollimatorAngle != 0)
                    {
                        msg += "\n\nFor the setup field \"" + scan.Id + "\", the collimator angle is not zero.";
                        foundbadSUgantry = true;
                    }
                    if (scan.Id.ToLower().Contains("cbct"))
                    {
                        if (scan.ControlPoints.First().GantryAngle != 0)
                        {
                            msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                            foundbadSUgantry = true;
                        }
                    }

                    //HFS
                    if (plan.TreatmentOrientation.ToString() == "HeadFirstSupine")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                foundbadSUgantry = true;
                            }
                        }

                    }
                    //FFS
                    if (plan.TreatmentOrientation.ToString() == "FeetFirstSupine")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                foundbadSUgantry = true;
                            }
                        }

                    }
                    //HFP
                    if (plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    {

                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            msg += "\n\nPatient is prone, did you mean to lable the 'ANT Setup' field 'POST Setup' instead?";
                            foundbadSUgantry = true;
                        }
                        if (scan.Id.ToLower().Contains("post"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                foundbadSUgantry = true;
                            }
                        }

                    }
                    //FFP
                    if (plan.TreatmentOrientation.ToString() == "FeetFirstProne")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            msg += "\n\nPatient is prone, did you mean to lable the 'ANT Setup' field 'POST Setup' instead?";
                            foundbadSUgantry = true;
                        }
                        if (scan.Id.ToLower().Contains("post"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                foundbadSUgantry = true;
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                foundbadSUgantry = true;
                            }
                        }
                    }
                }
            }
            if (foundbadSUgantry == true)
            {
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that field names begin with the correct numbers based on the name of the plan
            row = table.NewRow();
            row["Item"] = "The field names begin with the correct numbers based on the name of the plan(\"1.1\" for field 1, plan 1 etc.)";
            bool foundbadplannumber = false;
            if (plan.Id.StartsWith("FP1")) //FP stands for final plan
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("1.") == false)
                        {
                            msg += "\n\nPlan FP1 expects fields to start with '1.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("2.") == false)
                        {
                            msg += "\n\nPlan FP2 expects fields to start with '2.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("3.") == false)
                        {
                            msg += "\n\nPlan FP3 expects fields to start with '3.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP4"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("4.") == false)
                        {
                            msg += "\n\nPlan FP4 expects fields to start with '4.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP5"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("5.") == false)
                        {
                            msg += "\n\nPlan FP5 expects fields to start with '5.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P1")) //M represents a mod plan of plan 1
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-1.") == false)
                        {
                            msg += "\n\nPlan M1P1 expects fields to start with 'M1-1.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P1"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-1.") == false)
                        {
                            msg += "\n\nPlan M2P1 expects fields to start with 'M2-1.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M3P1"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M3-1.") == false)
                        {
                            msg += "\n\nPlan M3P1 expects fields to start with 'M3-1.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-2.") == false)
                        {
                            msg += "\n\nPlan M1P2 expects fields to start with 'M1-2.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-2.") == false)
                        {
                            msg += "\n\nPlan M2P2 expects fields to start with 'M2-2.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M3P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M3-2.") == false)
                        {
                            msg += "\n\nPlan M3P2 expects fields to start with 'M3-2.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-3.") == false)
                        {
                            msg += "\n\nPlan M1P3 expects fields to start with 'M1-3.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-3.") == false)
                        {
                            msg += "\n\nPlan M2P3 expects fields to start with 'M2-3.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P4"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-4.") == false)
                        {
                            msg += "\n\nPlan M1P4 expects fields to start with 'M1-4.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P5"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-5.") == false)
                        {
                            msg += "\n\nPlan M1P5 expects fields to start with 'M1-5.'";
                            foundbadplannumber = true;
                        }
                    }
                }
            }
            if (foundbadplannumber == true)
            {
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check names of fields (LAO/RAO etc) against gantry angles
            row = table.NewRow();
            row["Item"] = "Basic checks on field naming: i) Arcs have \"ARC\" in the name ii)Static fields have orientation \"LT\", \"ANT\", \"RAO\" etc. included. Checks for all four patient orientations.";
            bool foundfieldissue = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == false)
                {

                    if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                    {
                        if (scan.Id.ToLower().Contains("arc") == false)
                        {
                            msg += "\n\nFields of type \"ARC\" (\"" + scan.Id + "\") should contain \"ARC\" in the field name.";
                            foundfieldissue = true;
                        }
                    }
                    if (scan.Technique.Id.Equals("STATIC"))
                    {
                        if (plan.TreatmentOrientation.ToString() == "HeadFirstSupine")
                        {
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0 && !plan.Id.ToLower().Contains("sc+ax"))
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("post") && !plan.Id.ToLower().Contains("sc+ax"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    foundfieldissue = true;
                                }
                            }
                        }

                        if (plan.TreatmentOrientation.ToString() == "FeetFirstSupine")
                        {
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    foundfieldissue = true;
                                }
                            }
                        }

                        if (plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        {
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    foundfieldissue = true;
                                }
                            }
                        }

                        if (plan.TreatmentOrientation.ToString() == "FeetFirstProne")
                        {
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    foundfieldissue = true;
                                }
                            }
                            else if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    foundfieldissue = true;
                                }
                            }
                        }
                    }

                }
            }
            if (foundfieldissue == true)
            {
                row["Result"] = "Fail";
            }
            else if (foundfieldissue == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check to make sure that heterogeneity corrections are on
            row = table.NewRow();
            row["Item"] = "Heterogeneity corrections are on";
            if (plan.PhotonCalculationOptions.ContainsKey("HeterogeneityCorrection"))
            {
                string value = plan.PhotonCalculationOptions["HeterogeneityCorrection"];
                if (value.Equals("OFF"))
                {
                    msg += "\n\nHeterogeneity corrections are OFF.";
                    row["Result"] = "Fail";
                }
                else
                {
                    row["Result"] = "Pass";
                }
            }
            table.Rows.Add(row);

            // Check MU > 5
            row = table.NewRow();
            row["Item"] = "All beams have an MU setting of greater than 5 MU";
            foreach (Beam scan in listofbeams)
            {
                if (scan.Meterset.Value < 5)
                {
                    msg += "\n\nField \"" + scan.Id + "\" has fewer than 5 MU.";
                    row["Result"] = "Fail";
                }
                else
                {
                    row["Result"] = "Pass";
                }
            }
            table.Rows.Add(row);

            // Check AAA version
            row = table.NewRow();
            row["Item"] = "For photon plans, the dose calculation algorithm is \"AAA_15606\"";
            if (plan.PhotonCalculationModel != "AAA_15606")
            {
                msg += "\n\nThe photon calculation model is expected to be: AAA_15606, but is instead: " + plan.PhotonCalculationModel;
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check EMC version
            row = table.NewRow();
            row["Item"] = "For electron plans, the dose calculation algorithm is \"EMC_11031\"";
            bool isElectron = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.EnergyModeDisplayName.Equals("6E") || scan.EnergyModeDisplayName.Equals("9E") || scan.EnergyModeDisplayName.Equals("12E") || scan.EnergyModeDisplayName.Equals("16E") || scan.EnergyModeDisplayName.Equals("20E"))
                {
                    // Set electron flag to true
                    isElectron = true;
                }
            }

            if (isElectron == true)
            {
                if (plan.ElectronCalculationModel != "EMC_11031")
                {
                    msg += "\n\nThe electron calculation model is not set to: \"EMC_11031\"";
                    row["Result"] = "Fail";
                }
                else
                {
                    row["Result"] = "Pass";
                }
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            //Check for couch structure
            row = table.NewRow();
            row["Item"] = "The couch structure is correct for the selected treatment unit and has the correct HU values";
            int couchok = 2;
            if (Machine.Contains("TB"))
            {
                var foundcouch = false;
                var wrongcouch = false;
                var listofstructures = plan.StructureSet.Structures;
                foreach (Structure scan in listofstructures)
                {
                    if (scan.Name.Contains("Exact IGRT Couch"))
                    {
                        foundcouch = true;
                        bool structHU = scan.GetAssignedHU(out double huValue);
                        if (scan.Id.ToLower().Contains("interior"))
                        {
                            if (huValue != -960)
                            {
                                msg += "\n\nVarian couch structure found, but the interior HU is set to " + huValue + " when -960 was expected.";
                                row["Result"] = "Fail";
                                couchok = 0;
                            }
                        }
                        if (scan.Id.ToLower().Contains("surface"))
                        {
                            if (huValue != -700)
                            {
                                msg += "\n\nVarian couch structure found, but the exterior HU is set to " + huValue + " when -700 was expected.";
                                row["Result"] = "Fail";
                                couchok = 0;
                            }
                        }
                    }
                    if (scan.Name.Contains("BrainLAB"))
                    {
                        foundcouch = true;
                        wrongcouch = false;
                    }
                }
                if (foundcouch == false)
                {
                    msg += "\n\nVarian IGRT couch structure missing.";
                    row["Result"] = "Fail";
                    couchok = 0;
                }
                if (wrongcouch == true)
                {
                    msg += "\n\nWrong couch structure detected.";
                    row["Result"] = "Fail";
                    couchok = 0;
                }
                if (couchok == 2)
                {
                    row["Result"] = "Pass";
                }
            }

            if (Machine.Contains("STX"))
            {
                var foundcouch = false;
                var wrongcouch = false;
                var listofstructures = plan.StructureSet.Structures;
                foreach (Structure scan in listofstructures)
                {
                    if (scan.Name.Contains("BrainLAB"))
                    {
                        foundcouch = true;
                        bool structHU = scan.GetAssignedHU(out double huValue);
                        if (scan.Id.ToLower().Contains("interior"))
                        {
                            if (huValue != -850)
                            {
                                msg += "\n\nBrainLAB couch structure found, but the interior HU is set to " + huValue + " when -850 was expected.";
                                row["Result"] = "Fail";
                                couchok = 0;
                            }
                        }
                        if (scan.Id.ToLower().Contains("surface"))
                        {
                            if (huValue != -300)
                            {
                                msg += "\n\nBrainLAB couch structure found, but the exterior HU is set to " + huValue + " when -300 was expected.";
                                row["Result"] = "Fail";
                                couchok = 0;
                            }
                        }
                    }
                    if (scan.Name.Contains("Exact IGRT Couch"))
                    {
                        foundcouch = true;
                        wrongcouch = true;
                    }
                }
                if (foundcouch == false)
                {
                    msg += "\n\nBrainLAB couch structure missing, make sure patient is in the \"U-frame\" mask.";
                    //row["Result"] = "Fail"; //We don't want this to say fail because it can sometimes be correct. 
                    couchok = 1;
                }
                if (wrongcouch == true)
                {
                    msg += "\n\nWrong couch structure detected.";
                    row["Result"] = "Fail";
                    couchok = 0;
                }
                if (couchok == 2)
                {
                    row["Result"] = "Pass";
                }
            }
            table.Rows.Add(row);

            //Checks name of CT against name of structure set
            row = table.NewRow();
            row["Item"] = "The name of the structure set matches the name of the CT";
            if (plan.StructureSet.Id != plan.StructureSet.Image.Id)
            {
                msg += "\n\nCT name and structure set name do not match.";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            //Checks grid size is 0.1 cm for lung SBRT (Dose per fx > 9 and FS < 10 cm with "lung" in plan name) and brain cases.
            row = table.NewRow();
            row["Item"] = "The calculation grid size is 0.1 cm for lung SBRT (Dose per fx > 9 and FS < 10 cm with \"lung\" in plan name) and brain cases";
            int gridsizeok = 2;
            var foundoptics = false; // Brain (optic structures found in structure set)
            var listofstructures2 = plan.StructureSet.Structures;
            foreach (Structure scan in listofstructures2)
            {
                if (scan.Id.ToLower().Contains("optic") && (scan.IsEmpty == false))
                {
                    foundoptics = true;
                }
            }
            if (foundoptics == true && !plan.Id.ToLower().Contains("ent"))
            {
                if (plan.Dose != null)
                {
                    if (!plan.Dose.XRes.Equals(1))
                    {
                        msg += "\n\nThe plan contains optic structures, but the calculation grid size is not 0.1 cm. Is this intentional?";
                        row["Result"] = "Fail";
                        gridsizeok = 0;
                    }
                }
            }

            if (plan.Id.ToLower().Contains("lung")) // Lung SBRT
            {
                if (plan.DosePerFraction.Dose > 9)
                {
                    if (plan.Dose != null)
                    {
                        if (!plan.Dose.XRes.Equals(1))
                        {
                            int FSlessthanten = 0;
                            foreach (Beam scan in listofbeams)
                            {
                                if (scan.IsSetupField == false)
                                {
                                    var listofCP = scan.ControlPoints;
                                    foreach (ControlPoint cp in listofCP)
                                    {
                                        double X1 = cp.JawPositions.X1;
                                        double X2 = cp.JawPositions.X2;
                                        double Y1 = cp.JawPositions.Y1;
                                        double Y2 = cp.JawPositions.Y2;
                                        GetFieldSize(X1, X2, Y1, Y2, out double XFS, out double YFS);
                                        if (XFS < 100) //FS is in mm.
                                        {
                                            FSlessthanten = 1;
                                        }
                                        if (YFS < 100)
                                        {
                                            FSlessthanten = 1;
                                        }
                                    }
                                }
                            }
                            if (FSlessthanten == 1)
                            {
                                msg += "\n\nThe plan might be a lung SBRT case but the calculation grid size is not 0.1 cm. Is this intentional?";
                                row["Result"] = "Fail";
                                gridsizeok = 0;
                            }
                        }
                    }
                }
            }
            if (gridsizeok == 2)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Test for FFDA (tray present)
            row = table.NewRow();
            row["Item"] = "Electron beams all have a tray ID defined";
            var traysmissing = 0;
            foreach (Beam scan in listofbeams)
            {
                if (scan.EnergyModeDisplayName.Contains("E"))
                {
                    if (scan.Trays.Count() == 0)
                    {
                        if (traysmissing == 0)
                        {
                            traysmissing = 1;
                        }

                    }
                }
            }
            if (traysmissing == 1)
            {
                msg += "\n\nThe tray ID is not set in one or more electron block properties";
                row["Result"] = "Fail";
            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Test the grid size for electron plans. If it has 6 MeV then it should be 0.1 cm. Otherwise 0.15 cm.
            row = table.NewRow();
            row["Item"] = "The calculation grid size for electron plans is  0.1 cm if the plan has 6 MeV otherwise it should be 0.15 cm.";
            int egridsizeok = 2;

            var foundelectrons = 0;
            var found6MeV = 0;

            if (plan.Dose != null)
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.EnergyModeDisplayName.Contains("E"))
                    {
                        foundelectrons = 1;
                    }
                    if (scan.EnergyModeDisplayName.Contains("6E"))
                    {
                        found6MeV = 1;
                    }
                }

                if (foundelectrons.Equals(1) && found6MeV.Equals(1))
                {
                    if (!plan.Dose.XRes.Equals(1))
                    {
                        msg += "\n\nThe plan is an electron plan with a 6 MeV beam. The calculation grid size should be 0.1 cm.";
                        row["Result"] = "Fail";
                        egridsizeok = 0;
                    }
                }
                if (foundelectrons.Equals(1) && found6MeV.Equals(0))
                {
                    if (!plan.Dose.XRes.Equals(1.5))
                    {
                        msg += "\n\nThe plan is an electron plan with only energies greater than 6 MeV. The calculation grid size should be 0.15 cm.";
                        row["Result"] = "Fail";
                        egridsizeok = 0;
                    }
                }
            }

            if (egridsizeok == 2)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check for small targets below 2 cm diameter and advise that physics be consulted if it's below that limit.
            row = table.NewRow();
            row["Item"] = "The primary target is above approximately 2 cm in diameter.";

            if (plan.TargetVolumeID != "")
            {
                bool foundarc = false;
                foreach (Beam scan in listofbeams)
                {
                    if (scan.Technique.Id.Equals("ARC"))
                    {
                        foundarc = true;
                    }
                }
                if (foundarc == true)
                {
                    Structure structure = plan.StructureSet.Structures.Where(s => s.Id == plan.TargetVolumeID).Single();
                    if (structure.Volume < 4.18879) // This is the volume of a 2 cm wide sphere in cc.
                    {
                        double equivdiam = 2 * Math.Pow((3 * structure.Volume / (4 * Math.PI)), 1 / 3.0);
                        msg += "\n\nThe size of the primary target is quite small (" + structure.Volume.ToString("0.0") + " cc). This would have an eqivalent diameter sphere of " +
                            equivdiam.ToString("0.0") + " cm which is below the ~2 cm limit where we might start to see dose calculation accuracy issues. Please consult physics.";
                        row["Result"] = "Fail";
                    }
                    else
                    {
                        row["Result"] = "Pass";
                    }
                }
                else
                {
                    row["Result"] = "Pass";
                }

            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            //Checks that for any couch angles that are not zero, that the angle is in the field name in the form: "Txyz" where xyz is the angle. 
            row = table.NewRow();
            row["Item"] = "Non zero couch angles have the angle included in the field name.";
            int noncoplanarnamesok = 2;
            foreach (Beam scan in listofbeams)
            {

                if (!scan.EnergyModeDisplayName.Contains("E"))
                {
                    double couchangle = scan.ControlPoints.First().PatientSupportAngle;
                    string eclipsecouchangle = "";
                    string stringname = "";
                    if (couchangle != 0)
                    {
                        if (couchangle < 180)
                        {
                            eclipsecouchangle = (360 - couchangle).ToString();
                        }
                        else if (couchangle > 180)
                        {
                            eclipsecouchangle = (360 - couchangle).ToString();
                        }
                        stringname = "T" + eclipsecouchangle; // This should be included in the field name. Like "1.1 ARC1 T270" if the table is at 270 degrees.

                        if (!scan.Id.Contains(stringname))
                        {
                            msg += "\n\nField: \"" + scan.Id + "\", which has a table angle of " + eclipsecouchangle + " degrees, does not contain " + stringname + " in the field name.";
                            row["Result"] = "Fail";
                            noncoplanarnamesok = 0;
                        }
                    }
                }

            }
            if (noncoplanarnamesok == 2)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);


            // Check that the dose rate is the highest setting for all treatment fields.
            row = table.NewRow();
            row["Item"] = "The dose rate is set to the highest available value for each treatment beam.";
            int highestdoserate = 2;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == false)
                {
                    if (scan.EnergyModeDisplayName == "6X" || scan.EnergyModeDisplayName == "10X" || scan.EnergyModeDisplayName == "15X")
                    {
                        if (scan.DoseRate != 600)
                        {
                            msg += "\n\nField: \"" + scan.Id + "\", has a dose rate of " + scan.DoseRate.ToString() + " MU/min instead of the maximum of 600 MU/min";
                            highestdoserate = 0;
                        }
                    }
                    if (scan.EnergyModeDisplayName == "6X-FFF")
                    {
                        if (scan.DoseRate != 1400)
                        {
                            msg += "\n\nField: \"" + scan.Id + "\", has a dose rate of " + scan.DoseRate.ToString() + " MU/min instead of the maximum of 1400 MU/min";
                            highestdoserate = 0;
                        }
                    }
                    if (scan.EnergyModeDisplayName == "10X-FFF")
                    {
                        if (scan.DoseRate != 2400)
                        {
                            msg += "\n\nField: \"" + scan.Id + "\", has a dose rate of " + scan.DoseRate.ToString() + " MU/min instead of the maximum of 2400 MU/min";
                            highestdoserate = 0;
                        }
                    }
                    if (scan.EnergyModeDisplayName == "6E" || scan.EnergyModeDisplayName == "9E" || scan.EnergyModeDisplayName == "12E" || scan.EnergyModeDisplayName == "16E" || scan.EnergyModeDisplayName == "20E")
                    {
                        if (scan.DoseRate != 1000)
                        {
                            msg += "\n\nField: \"" + scan.Id + "\", has a dose rate of " + scan.DoseRate.ToString() + " MU/min instead of the maximum of 1000 MU/min";
                            highestdoserate = 0;
                        }
                    }

                }
            }
            if (highestdoserate == 2)
            {
                row["Result"] = "Pass";
            }
            else if (highestdoserate == 0)
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);


            /////////////////////////////////////////////////////////////////////////////
            // Write back current message and datatable
            SomeProperties.MsgString = msg;
            SomeProperties.MsgDataTable = table;
        }

        // Some method to get the field size.
        public static void GetFieldSize(double X1, double X2, double Y1, double Y2, out double XFS, out double YFS)
        {
            XFS = X2 - X1;
            YFS = Y2 - Y1;
        }
    }
}
