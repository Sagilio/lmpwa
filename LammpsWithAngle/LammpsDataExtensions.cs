﻿using System;
using LammpsWithAngle.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Serilog;

namespace LammpsWithAngle
{
    public static class LammpsDataExtensions
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static LammpsData CompleteBondAndAngle(this LammpsData lammpsData, string waterModel, bool large27, bool fixInvalidAxis)
        {
            var charges = Charges.GetCharges(waterModel);
            lammpsData.Bonds.Clear();
            lammpsData.Angles.Clear();

            var atoms = new List<Atom>();
            var anotherAtoms = large27 ? lammpsData.Atoms.Large27(lammpsData) : lammpsData.Atoms;

            var nowAngleId = 0;
            var nowAtomId = 0;
            var nowChainId = 0;
            var nowBondId = 0;
            foreach (Atom atomO in lammpsData.Atoms.Where(a => a.Type == (int)AtomType.O))
            {
                var waterAtoms = new List<Atom>();
                var waterBonds = new List<Bond>();
                int waterAtomId = nowAtomId;
                int waterChainId = nowChainId;
                int waterBondId = nowBondId;

                waterAtomId++;
                waterChainId++;
                int waterOId = waterAtomId;
                waterAtoms.Add(new Atom
                {
                    Id = waterAtomId,
                    Chain = waterChainId,
                    Type = (int)AtomType.O,
                    Charge = charges.O,
                    X = atomO.X,
                    Y = atomO.Y,
                    Z = atomO.Z
                });

                foreach (var atomH in anotherAtoms.Where(a => a.Type == (int)AtomType.H))
                {
                    double distanceOH = Math.Sqrt(
                        (atomO.X - atomH.X) * (atomO.X - atomH.X) +
                        (atomO.Y - atomH.Y) * (atomO.Y - atomH.Y) +
                        (atomO.Z - atomH.Z) * (atomO.Z - atomH.Z));

                    if (distanceOH is <= 0.5 or >= 1.05)
                    {
                        continue;
                    }

                    waterAtomId++;
                    waterAtoms.Add(new Atom
                    {
                        Id = waterAtomId,
                        Chain = waterChainId,
                        Type = (int)AtomType.H,
                        Charge = charges.H,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });

                    waterBondId++;
                    waterBonds.Add(new Bond
                    {
                        Id = waterBondId,
                        Type = (int)AtomType.O,
                        AtomId1 = waterOId,
                        AtomId2 = waterAtomId
                    });
                }

                if (waterAtoms.Count != 3)
                {
                    Log.Logger.Information("Remove error water atoms of ids: {0}.",
                            string.Join(", ", waterAtoms.Select(a => a.Id)));
                    continue;
                }

                atoms.AddRange(waterAtoms);
                lammpsData.Bonds.AddRange(waterBonds);

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.O,
                    AtomId1 = waterAtomId - 1,
                    AtomId2 = waterOId,
                    AtomId3 = waterAtomId
                });

                lammpsData.WaterCount++;
                nowAtomId = waterAtomId;
                nowChainId = waterChainId;
                nowBondId = waterBondId;
            }

            foreach (Atom atomC in lammpsData.Atoms.Where(a => a.Type == (int)AtomType.C))
            {
                var methaneAtoms = new List<Atom>();
                var methaneBonds = new List<Bond>();
                int methaneAtomId = nowAtomId;
                int methaneChainId = nowChainId;
                int methaneBondId = nowBondId;

                methaneAtomId++;
                methaneChainId++;
                int methaneAtomCId = methaneAtomId;
                methaneAtoms.Add(new Atom
                {
                    Id = methaneAtomId,
                    Chain = methaneChainId,
                    Type = (int)AtomType.C,
                    Charge = charges.C,
                    X = atomC.X,
                    Y = atomC.Y,
                    Z = atomC.Z
                });

                foreach (var atomH in anotherAtoms.Where(a => a.Type == (int)AtomType.H))
                {
                    double distanceCH = Math.Sqrt(
                        (atomC.X - atomH.X) * (atomC.X - atomH.X) +
                        (atomC.Y - atomH.Y) * (atomC.Y - atomH.Y) +
                        (atomC.Z - atomH.Z) * (atomC.Z - atomH.Z));

                    if (distanceCH is <= 0.5 or >= 1.2)
                    {
                        continue;
                    }

                    methaneAtomId++;
                    methaneAtoms.Add(new Atom
                    {
                        Id = methaneAtomId,
                        Chain = methaneChainId,
                        Type = (int)AtomType.H_CH4,
                        Charge = charges.H_CH4,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });

                    methaneBondId++;
                    methaneBonds.Add(new Bond
                    {
                        Id = methaneAtomId,
                        Type = (int)AtomType.H,
                        AtomId1 = methaneAtomCId,
                        AtomId2 = methaneAtomId
                    });
                }

                if (methaneAtoms.Count is not 5)
                {
                    Log.Logger.Information("Remove error methane atoms of ids: {0}.",
                        string.Join(", ", methaneAtoms.Select(a => a.Id)));
                    continue;
                }

                atoms.AddRange(methaneAtoms);
                lammpsData.Bonds.AddRange(methaneBonds);

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 1,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 2,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 3,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 3,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 1
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 2,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 1
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int)AtomType.H,
                    AtomId1 = methaneAtomId - 2,
                    AtomId2 = methaneAtomCId,
                    AtomId3 = methaneAtomId - 3
                });

                lammpsData.MethaneCount++;
                nowAtomId = methaneAtomId;
                nowBondId = methaneBondId;
                nowChainId = methaneChainId;
            }

            if (fixInvalidAxis)
            {
                foreach (var atom in atoms)
                {
                    atom.FixInvalidAxis(lammpsData);
                }
            }

            lammpsData.Atoms = atoms;
            lammpsData.ChainsCount = nowChainId;
            lammpsData.AtomTypeCount = 4;
            lammpsData.BondTypeCount = 2;
            lammpsData.AngleTypeCount = 2;
            return lammpsData;
        }

        private static List<Atom> Large27(this ICollection<Atom> sourceAtoms, LammpsData lammpsData)
        {
            var atoms = new List<Atom>();
            var nowAtomId = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        foreach (var atom in sourceAtoms)
                        {
                            atoms.Add(new Atom
                            {
                                Id = nowAtomId,
                                Type = atom.Type,
                                X = atom.X + x * (lammpsData.Xhi - lammpsData.Xlo),
                                Y = atom.Y + y * (lammpsData.Yhi - lammpsData.Ylo),
                                Z = atom.Z + z * (lammpsData.Zhi - lammpsData.Zlo)
                            });
                            nowAtomId++;
                        }
                    }
                }
            }

            return atoms;
        }

        private static Atom FixInvalidAxis(this Atom atom, LammpsData lammpsData)
        {
            if (atom.X < 0)
            {
                double oldX = atom.X;
                atom.X += (lammpsData.Xhi - lammpsData.Xlo);
                Log.Logger.Information("Fixed invalid X {0} to {1}", 
                    oldX, atom.X);
            }
            if (atom.Y < 0)
            {
                double oldY = atom.Y;
                atom.Y += (lammpsData.Yhi - lammpsData.Ylo);
                Log.Logger.Information("Fixed invalid Y {0} to {1}", 
                    oldY, atom.Y);
            }
            if (atom.Z < 0)
            {
                double oldZ = atom.Z;
                atom.Z += (lammpsData.Zhi - lammpsData.Zlo);
                Log.Logger.Information("Fixed invalid Z {0} to {1}", 
                    oldZ, atom.Z);
            }
            return atom;
        }
    }
}