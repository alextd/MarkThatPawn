using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MarkThatPawn;

public class MarkerDef : Def
{
    private bool? enabled;
    private string graphicPrefix;
    private Texture2D icon;
    private string iconPath;
    private List<Material> markerMaterials;


    private List<Texture2D> markerTextures;
    private List<string> mayRequire;

    public List<Material> MarkerMaterials
    {
        get
        {
            if (markerMaterials == null)
            {
                loadTextures();
            }

            return markerMaterials;
        }
    }

    public List<Texture2D> MarkerTextures
    {
        get
        {
            if (markerTextures == null)
            {
                loadTextures();
            }

            return markerTextures;
        }
    }

    public Texture2D Icon
    {
        get
        {
            if (icon == null)
            {
                loadTextures();
            }

            return icon;
        }
    }

    public bool Enabled
    {
        get
        {
            if (enabled != null)
            {
                return (bool)enabled;
            }

            if (mayRequire == null || mayRequire.Any() == false)
            {
                enabled = true;
                return true;
            }

            foreach (var modId in mayRequire)
            {
                if (ModLister.GetActiveModWithIdentifier(modId) != null)
                {
                    continue;
                }

                enabled = false;
                return false;
            }

            enabled = true;
            return true;
        }
    }

    private void loadTextures()
    {
        icon = ContentFinder<Texture2D>.Get(iconPath, false);
        markerTextures = [];
        markerMaterials = [];

        var counter = 0;
        for (var i = 0; i < 100; i++)
        {
            counter++;
            var foundTexture = ContentFinder<Texture2D>.Get($"{graphicPrefix}_{counter}", false);
            if (foundTexture == null)
            {
                break;
            }

            MarkerTextures.Add(foundTexture);
            MarkerMaterials.Add(MaterialPool.MatFrom($"{graphicPrefix}_{counter}", ShaderDatabase.MetaOverlay));
        }

        if (MarkerTextures.Any())
        {
            //Log.Message($"[MarkThatPawn]: Found {MarkerTextures.Count} icons for {LabelCap}");
        }
    }
}