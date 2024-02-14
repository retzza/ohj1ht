using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author Reetta Koskelo
/// @version 22.11.2018
/// <summary>
/// Tasohyppelypeli, jossa pelaaja kerää opintopisteitä. 
/// </summary>
public class Opintopistepeli : PhysicsGame
{
    private Image[] opintopisteet = LoadImages("1op_2", "2op_2");
    private Image astin = LoadImage("astin2");
    private Image trampoliini = LoadImage("trampoliini2");

    private List<PhysicsObject> astinlaudat = new List<PhysicsObject>();
    private List<PhysicsObject> pistetahdet = new List<PhysicsObject>();
    private List<PhysicsObject> trampoliinit = new List<PhysicsObject>();

    private const int PELIKENTAN_LOHKO = 700;
    private const int ASTIMIEN_MAARA = 200;
    private const double OPINTOPISTEIDEN_TN = 0.65;
    private const double TRAMPOLIINIEN_TN = 0.9;
    private const int KELAN_RAJA = 45;
    private const int YLIOPISTON_RAJA = 60;
    private const double PELAAJAN_KORKEUS = 80.0;
    private const double ASTIMEN_LEVEYS = 150.0; 
    private const int KENTAN_KORKEUS = 40000;

    private Vector nopeusOikealle = new Vector(300, 0);
    private Vector nopeusVasemmalle = new Vector(-300, 0);

    private IntMeter laskuri;

    private PlatformCharacter pelaaja;

    public override void Begin()
    {
        MultiSelectWindow valikko = new MultiSelectWindow("Tervetuloa yliopistoon! Olet varmasti kuullut KELAsta, joka rahoittaa opintosi." +
            Environment.NewLine + "Jotta et joudu maksamaan tukiasi takaisin, kerää " + KELAN_RAJA + " opintopistettä." +
            Environment.NewLine + "Halutessasi voit kerätä myös yliopiston suosittelemat " + YLIOPISTON_RAJA + " opintopistettä.", "Aloita peli");
        valikko.ItemSelected += (valinta) => { AloitaPeli(); };
        valikko.DefaultCancel = 0;
        Add(valikko);
    }


    /// <summary>
    /// Aloittaa pelin.
    /// </summary>
    private void AloitaPeli()
    {
        astinlaudat.Clear();
        pistetahdet.Clear();
        trampoliinit.Clear();
        ClearAll();
        LuoKentta();
        AsetaOhjaimet();
        LisaaLaskuri();
    }


    /// <summary>
    /// Luo pelikentän.
    /// </summary>
    private void LuoKentta()
    {
        Level.Height = KENTAN_KORKEUS;
        pelaaja = LuoPelaaja(Level.Bottom + PELAAJAN_KORKEUS / 2);

        Camera.Follow(pelaaja);
        Level.CreateBottomBorder();
        
        Level.CreateLeftBorder();
        Level.CreateRightBorder();
        Level.BackgroundColor = Color.White;

        Gravity = new Vector(0, -700);


        // Luodaan astinlautoja ja lisätään ne listaan "astinlaudat"
        for (int i = 0; i < ASTIMIEN_MAARA; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                PhysicsObject al = LuoAstinlauta(new Vector(RandomGen.NextDouble(Level.Left + ASTIMEN_LEVEYS / 2, Level.Right - ASTIMEN_LEVEYS / 2),
                        RandomGen.NextDouble(Level.Bottom + i * 1, Level.Bottom + (i + 1) * PELIKENTAN_LOHKO)));
                astinlaudat.Add(al);
            }
        }
        
        
        // Luodaan opintopisteitä ja lisätään ne listaan "pistetahdet"
        for (int i = 0; i < astinlaudat.Count; i++)
        {
            if (LuodaankoOlio() > OPINTOPISTEIDEN_TN)
            {
                PhysicsObject op;
                op = LuoOpintopiste(this,
                                    RandomGen.NextDouble(astinlaudat[i].X - ASTIMEN_LEVEYS  / 3, astinlaudat[i].X + ASTIMEN_LEVEYS / 3),
                                    astinlaudat[i].Y,
                                    50);
                op.Tag = "opintopiste";
                pistetahdet.Add(op);
            }
        }


        // Luodaan trampoliineja ja lisätään ne listaan "trampoliinit"
        for (int i = 0; i < astinlaudat.Count; i++)
        {
            if (LuodaankoOlio() > TRAMPOLIINIEN_TN)
            {
                PhysicsObject tramppa;
                tramppa = LuoTrampoliini(this, astinlaudat[i].X, astinlaudat[i].Y);
                tramppa.Tag = "trampoliini";
                trampoliinit.Add(tramppa);
            }
        }
    }


    /// <summary>
    /// Luo pelihahmon.
    /// </summary>
    /// <param name="y">Pelihahmon y-koordinaatti pelin alussa.</param>
    /// <returns>Pelihahmon</returns>
    private PlatformCharacter LuoPelaaja(double y)
    {
        PlatformCharacter pelaaja = new PlatformCharacter(40.0, PELAAJAN_KORKEUS);
        pelaaja.Image = LoadImage("pelaaja2");
        pelaaja.Y = y;
        pelaaja.Restitution = 0.1;
        AddCollisionHandler(pelaaja, "opintopiste", KasittelePelaajanTormays);
        AddCollisionHandler(pelaaja, "trampoliini", KasitteleTormaysTrampoliiniin);
        Add(pelaaja, 1);
        pelaaja.CanRotate = false;
        return pelaaja;
    }


    /// <summary>
    /// Luo peliin astinlaudan annettuihin koordinaatteihin.
    /// </summary>
    /// <param name="x">x-koordinaatti</param>
    /// <param name="y">y-koordinaatti</param>
    /// <returns></returns>
    private PhysicsObject LuoAstinlauta(Vector paikka)
    { 
        PhysicsObject astinlauta = new PhysicsObject(ASTIMEN_LEVEYS, 10);
        astinlauta.Position = paikka;
        astinlauta.Image = astin;
        astinlauta.MakeStatic();
        astinlauta.Tag = "astin";
        Add(astinlauta);
        return astinlauta;
    }


    /// <summary>
    /// Luo peliin kerättävät opintopisteet
    /// </summary>
    /// <param name="peli">Peli, johon pisteet luodaan</param>
    /// <param name="x">x-koodrinaatti</param>
    /// <param name="y">y-koordinaatti</param>
    /// <param name="a">koko</param>
    /// <returns>opintopisteen</returns>
    private PhysicsObject LuoOpintopiste(Game peli, double x, double y, double a)
    {
        PhysicsObject piste = new PhysicsObject(a, a);
        piste.Position = new Vector(x, y + a/2);
        int k = RandomGen.NextInt(opintopisteet.Length);
        piste.Image = opintopisteet[k];
        piste.MakeStatic();
        piste.IgnoresCollisionResponse = true;
        peli.Add(piste);
        return piste;
    }


    /// <summary>
    /// Luo peliin trampoliinin haluttuun paikkaan.
    /// </summary>
    /// <param name="peli">Peli, johon trampoliini luodaan</param>
    /// <param name="x">x-koordinaatti</param>
    /// <param name="y">y-koordinaatti</param>
    /// <returns></returns>
    private PhysicsObject LuoTrampoliini(Game peli, double x, double y)
    {
        PhysicsObject tramppa = new PhysicsObject(120, 30);
        tramppa.Position = new Vector(x, y + 15);
        tramppa.Image = trampoliini;
        tramppa.MakeStatic();
        peli.Add(tramppa);
        return tramppa;
    }


    /// <summary>
    /// Lisää peliin pistelaskurin
    /// </summary>
    /// <returns>pistelaskurin</returns>
    private IntMeter LuoLaskuri()
    {
        IntMeter laskuri = new IntMeter(0);
        laskuri.MaxValue = 180;

        Label naytto = new Label();
        naytto.BindTo(laskuri);
        naytto.X = Screen.Right - 100;
        naytto.Y = Screen.Top - 100;
        naytto.TextColor = Color.Black;
        naytto.BorderColor = Level.Background.Color;
        naytto.Color = Level.Background.Color;
        Add(naytto);

        return laskuri;
    }


    /// <summary>
    /// Lisää peliin laskurin.
    /// </summary>
    private void LisaaLaskuri()
    {
        laskuri = LuoLaskuri();
    }


    /// <summary>
    /// Asettaa peliin näppäinkomennot.
    /// </summary>
    private void AsetaOhjaimet()
    {
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, null);
        Keyboard.Listen(Key.F2, ButtonState.Pressed, YritaUudelleen, "Yritä uudelleen");

        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppy, "Pelaaja hyppää");
        Keyboard.Listen(Key.Space, ButtonState.Pressed, Hyppy, "Pelaaja hyppää");

        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaa, "Pelaaja liikkuu vasemmalle", pelaaja, nopeusVasemmalle);
        // Keyboard.Listen(Key.Left, ButtonState.Released, LiikutaPelaajaa, null, pelaaja, Vector.Zero);

        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaa, "Pelaaja liikkuu oikealle", pelaaja, nopeusOikealle);
        // Keyboard.Listen(Key.Right, ButtonState.Released, LiikutaPelaajaa, null, pelaaja, Vector.Zero);
    }


    /// <summary>
    /// Asettaa pelaajalle nopeuden.
    /// </summary>
    /// <param name="pelaaja">Liikuttamisen kohde</param>
    /// <param name="nopeus">Nopeusvektori</param>
    private void LiikutaPelaajaa(PlatformCharacter pelaaja, Vector nopeus)
    {
        pelaaja.Move( new Vector( nopeus.X, nopeus.Y) );
    }


    /// <summary>
    /// Saa pelaajan hyppäämään.
    /// </summary>
    private void Hyppy()
    {
        pelaaja.Jump(950);
    }


    /// <summary>
    /// Päivittää olioiden läpinmentävyyden
    /// </summary>
    /// <param name="time">aika</param>
    protected override void Update(Time time)
    {
        TeeOlioLapimentavaksi(astinlaudat);
        TeeOlioLapimentavaksi(trampoliinit);
        base.Update(time);
    }


    /// <summary>
    /// Tekee jokaisesta listan alkiosta joko läpimentävän tai ei.
    /// </summary>
    /// <param name="lista">Lista, jonka alkiot tehdään läpimentäviksi.</param>
    private void TeeOlioLapimentavaksi(List<PhysicsObject> lista)
    {
        foreach (PhysicsObject alkio in lista)
        {
            if (pelaaja.Y < alkio.Y)
                alkio.IgnoresCollisionResponse = true;
            else alkio.IgnoresCollisionResponse = false;
        }
    }


    /// <summary>
    /// Käsittelee pelaajan törmäykset opintopisteisiin.
    /// </summary>
    /// <param name="pelaaja">pelaaja, joka törmää</param>
    /// <param name="kohde">kohde, johon pelaaja törmää</param>
    private void KasittelePelaajanTormays(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        if (kohde.Image == opintopisteet[0])
        {
            laskuri.Value += 1;
        }

        if (kohde.Image == opintopisteet[1])
        {
            laskuri.Value += 2;
        }
        kohde.Destroy();
        if (laskuri.Value == KELAN_RAJA || laskuri.Value == KELAN_RAJA + 1) Lopetus();
        if (laskuri.Value == YLIOPISTON_RAJA || laskuri.Value == YLIOPISTON_RAJA + 1) Lopetus();
    }


    /// <summary>
    /// Käsittelee pelaajan törmäykset trampoliiniin.
    /// </summary>
    /// <param name="pelaaja">pelaaja, joka törmää</param>
    /// <param name="kohde">kohde, johon pelaaja törmää</param>
    private void KasitteleTormaysTrampoliiniin(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        if (pelaaja.Velocity.Y <= 0)
        {
            pelaaja.Hit(new Vector(0, 6000));
            kohde.Destroy();
        }
        else return;
    }


    /// <summary>
    /// Arpoo luvun liukuluvun väliltä [0, 1].
    /// </summary>
    /// <returns>satunnaisen liukuluvun</returns>
    private double LuodaankoOlio()
    {
        return RandomGen.NextDouble(0, 1);
    }


    /// <summary>
    /// Luo pelille lopun.
    /// </summary>
    private void Lopetus()
    {
        MultiSelectWindow valikko;
        if (laskuri.Value == KELAN_RAJA || laskuri.Value == KELAN_RAJA + 1)
        {
            valikko = new MultiSelectWindow("Onnittelut! Keräsit " + KELAN_RAJA + " opintopistettä, joten Kela ei peri tukia takaisin!" +
                Environment.NewLine + "Jos haluat, voit jatkaa opintojasi " + YLIOPISTON_RAJA + " op asti.", "Jatka pelaamista", "Lopeta");
            valikko.ItemSelected += PainettiinVoittovalikonNappia;
            valikko.DefaultCancel = 1;
            Add(valikko);
        }
        else if (laskuri.Value >= YLIOPISTON_RAJA)
        {
            valikko = new MultiSelectWindow("Onnittelut! Keräsit " + YLIOPISTON_RAJA + " opintopistettä!", "Lopeta");
            valikko.ItemSelected += (valinta) => { Exit(); };
            valikko.DefaultCancel = 0;
            Add(valikko);
        }
    }


    /// <summary>
    /// Valitsee toiminnon sen mukaan, mitä valikon nappia painettiin.
    /// </summary>
    /// <param name="valinta">Nappi, jota painettiin</param>
    private void PainettiinVoittovalikonNappia(int valinta)
    {
        switch (valinta)
        {
            case 0:
                JatkaPelia();
                break;
            case 1:
                Exit();
                break;
        }
    }


    /// <summary>
    /// Jatkaa peliä.
    /// </summary>
    private void JatkaPelia()
    { 
    }


    /// <summary>
    /// Kysyy, haluaako pelaaja aloittaa pelin alusta.
    /// </summary>
    private void YritaUudelleen()
    {
        MultiSelectWindow valikko = new MultiSelectWindow("Haluatko yrittää uudelleen?", "Kyllä", "Ei", "Lopeta");
        valikko.ItemSelected += PainettiinUusiYtitysValikonNappia;
        valikko.DefaultCancel = 1;
        Add(valikko);
    }


    /// <summary>
    /// Valitsee toiminnon sen mukaan, mitä nappia painettiin.
    /// </summary>
    /// <param name="valinta">Nappi, jota painettiin</param>
    private void PainettiinUusiYtitysValikonNappia(int valinta)
    {
        switch (valinta)
        {
            case 0:
                AloitaPeli();
                break;
            case 1:
                JatkaPelia();
                break;
            case 2:
                Exit();
                break;
        }
    }
}
