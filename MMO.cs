using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

//mm-studios
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator {

    [Description("Martin Miller Oscilator")]
    public class MMO : Indicator {
		private double wick_length=0.70;
		private int	period	= 14;
		private Estrategia strategy=Estrategia.Vicente;
		private int activity_interval_from=80000; //8:00 AM
		private int activity_interval_to=220000; //10:00 PM
		
		private int macd_slow_period=26;
		private int macd_fast_period=12;
		private int macd_signal_period=9;

		private double MACD_umbral_venta=0.4; // configurable entre 0 y 1
		private double MACD_umbral_compra=-0.4; // configurable entre 0 y -1
		private int trend_MACD_diff_period=3; //numero de barras usadas en el calculo de la tendencia de la linea MACD.Diff

		private int estocasticoA_D=3;
		private int estocasticoA_K=14;
		private int estocasticoA_smooth=3;
		private int estocasticoA_valor_periodo=1;  //min

		private double estocasticoA_umbral_venta=85;
		private double estocasticoA_umbral_compra=15;
		private LineaEstocastico linea_estocasticoA=LineaEstocastico.D;


		private int estocasticoB_D=3;
		private int estocasticoB_K=14;
		private int estocasticoB_smooth=3;
		private int estocasticoB1_valor_periodo=5;  //min
		private int estocasticoB2_valor_periodo=3;  //min
		private int estocasticoB3_valor_periodo=10;  //min

		private double estocasticoB_umbral_venta=70;
		private double estocasticoB_umbral_compra=30;
		private LineaEstocastico linea_estocasticoB=LineaEstocastico.D;


		
		private int rsi_period=14;
		private int rsi_smooth=3;

		private double RSI_umbral_venta=70;
		private double RSI_umbral_compra=30;

		//private int STORSI_period=14;

		private int stochasticsrsi_period=14;


		private int ATR_period=14;
		private double ATR_amplificacion=2.0;

		private int adx_period=14;
		private double ADX_umbral_minimo=20;



//		private int stochastics5m_sD_period=3;
//		private int stochastics5m_fK_period=14;
//		private int stochastics5m_sK_period=3;




		//-------------------gestion contratos
		private int enter_contratos=3;
		private double stop_loss=2.00;  //Parámetro Stop Loss, entre 1 y 3 puntos de diferencia con el precio de entrada
		private double stop_profit=1.00;
		private double trailing_step=2.00;  ///


		private CondicionSalida condicion_salida_contrato1=CondicionSalida.stop_profit;
		private CondicionSalida condicion_salida_contrato2=CondicionSalida.estocastico_cruza_nivel;
		private CondicionSalida condicion_salida_contrato3=CondicionSalida.estocastico_gira;


		private bool ruleL1=true;
		private bool ruleL2=true;
		private bool ruleL3=true;
		private bool ruleL4=true;
		private bool ruleL5=true;
		private bool ruleL6=true;  ///RSI
		private bool ruleL7=true; ///STORSI
		private bool ruleL8=true; ///ADX
		private bool ruleL9=true; ///Stochastic B

		private int L6_RSI_velas=3;
		private int L9_num_estocasticos=1;


		private bool ruleV1=true; //vol
		private bool ruleV2=true; //giro
		private bool ruleV2_1=true; 
		private bool ruleV2_2=true; 
		private bool ruleV2_3=true; 
		private bool ruleV2_4=true; 
		
//----------------------------------------------------------------/ parameters
		private bool write_MMOlog=false;


		private int ciclo=0;
		private int num_entradas=0;
		private int num_contratos_ganadores=0;
		private int num_contratos_perdedores=0;
		private double pl=0;
		private Font textfont=new Font("Arial Narrow",10);




		protected override void Initialize() {
			
			//CalculateOnBarClose = true;

		   Plot short_plot=new Plot(new Pen(Color.LightPink,5), PlotStyle.Bar, "EnterShort");
//		   short_plot.Min=-4;
//		   short_plot.Max=0;
           Add(short_plot);
		 
		   Plot long_plot=new Plot(new Pen(Color.LightBlue,5), PlotStyle.Bar, "EnterLong");
//		   long_plot.Min=0;
//		   long_plot.Max=4;
           Add(long_plot);
		   
		   Add(new Line(Color.Gray,0,"Noop"));
           Overlay=false;

			
			//http://www.ninjatrader.com/support/helpGuides/nt7/index.html?multi_time_frame__instruments.htm
//			Add(PeriodType.Minute, 5); //becomes BarsArray[1]
			Add(PeriodType.Minute, estocasticoB1_valor_periodo); //becomes BarsArray[1]
			Add(PeriodType.Minute, estocasticoA_valor_periodo); //becomes BarsArray[2]
			Add(PeriodType.Minute, estocasticoB2_valor_periodo); //becomes BarsArray[1]
			Add(PeriodType.Minute, estocasticoB3_valor_periodo); //becomes BarsArray[1]

			//Add(PeriodType.Second, 10); //becomes BarsArray[1]
			
			if (write_MMOlog) {
				Print("I");
				string fn=Cbi.Core.UserDataDir.ToString() + "MMOlog.txt";
				Print(fn);
			
				using(System.IO.StreamWriter file = new System.IO.StreamWriter(Cbi.Core.UserDataDir.ToString() + "MMOlog.txt")) {
					Print("CREado MMOlog");
					file.WriteLine("NEW FILE");
				}
				write_params();
			}		
        }



		public struct decision {
			public bool sell;
			public bool buy;
		}

		//-------------------------------------------------------------------------------------luis
/*

// 3) Macd por encima de un parámetro (nunca menos de 0, máximo 6)
cambios:
antes: 	MACD, umbral venta, valor entre 0 y 1
despues: MACD, umbral venta, valor entre 0 y 6

antes: MACD, umbral compra, valor entre 0 y -1
despues: MACD, umbral compra, valor entre 0 y -6





*/
		public bool trending_up(IDataSeries data,int numbars,int endbar) {
			double v=data[endbar];
			for (int i=endbar+1; i<endbar+numbars; ++i) {
				if (data[i]>v) return false;
				v=data[i];
			}
			return true;
		}
		public bool trending_down(IDataSeries data,int numbars,int endbar) {
			double v=data[endbar];
			for (int i=endbar+1; i<endbar+numbars; ++i) {
				if (data[i]<v) return false;
				v=data[i];
			}
			return true;
		}

/*
		public bool filter_1(Stochastics sto) {
			Console.Write("Filtro 1: ");
			if (!filter1) {
				Console.WriteLine("off");
				return true;
			}

			//1) Que el indicador estocástico este por encima de un nivel configurable y venga de más arriba. Cuando empiece a caer.
			//1) Estocastico mayor de un parámetro (nunca menos de 80)
			if (sto.D[0]>estocastico_umbral_venta) {  
				Print("+Stochastics D is above the limit at "+estocastico_umbral_venta);

			return false;
		}
*/

		public bool rule_L1(bool sell, Stochastics stoA) {
			string rule="  Regla L1: ";
			string desc="Estocastico A rebasa umbral";
			if (!ruleL1) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}
			double v=0;
			if (linea_estocasticoA==LineaEstocastico.D) {
				v=stoA.D[0];
			}
			if (linea_estocasticoA==LineaEstocastico.K) {
				v=stoA.K[0];
			}
			if (sell) {
				desc+=" sto > "+estocasticoA_umbral_venta;
				//1) Que el indicador estocástico este por encima de un nivel configurable y venga de más arriba. Cuando empiece a caer.
				//1) Estocastico mayor de un parámetro (nunca menos de 80)
				if (v>estocasticoA_umbral_venta) {  
					Print(rule+"[si] " + desc);
					return true; 
				}
			}
			else {
				desc+=" sto < "+estocasticoA_umbral_compra;
				//1) Que el indicador estocástico este por debajo de un nivel configurable y venga de más abajo. Cuando empiece a crecer.
				//1) Estocastico menor de un parámetro (nunca mas de 20)
				if (v<estocasticoA_umbral_compra) {  
					Print(rule+"[si] " + desc);
					return true;  
				}
			}
			Print(rule+"[no] " + desc);

			return false;
		}

		public bool rule_L2(bool sell, Stochastics stoA) {
			string rule="  Regla L2: ";
			string desc="Estocastico acaba de girar";
			if (!ruleL2) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}
			DataSeries S=stoA.D;
			if (linea_estocasticoA==LineaEstocastico.K) {
				S=stoA.K;
			}

			if (sell) {
				//2) Estocástico (D) vela 0 menor que estocástico (D) vela 1
				if (S[0]<S[1]) {  //y decreciendo  
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			else {
				//2) Estocástico (D) vela 0 mayor que estocástico (D) vela 1
				if (S[0]>S[1]) {  //y creciendo  
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}

			return false;
		}

		public bool rule_L3(bool sell, MACD macd) {
			string rule="  Regla L3: ";
			string desc="MACD rebasa umbral";
			if (!ruleL3) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (sell) {
				// 3) Que el indicador macd esté por encima de un nivel configurable
				// 3) Macd por encima de un parámetro (nunca menos de 0, máximo 6)
				if (macd[0]>MACD_umbral_venta) {
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			else {
				// 3) Que el indicador macd esté por debajo de un nivel configurable
				// 3) Macd por debajo de un parámetro (nunca mas de 0, mínimo -6)
				if (macd[0]<MACD_umbral_compra) {  //-0.4
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			return false;
		}

		public bool rule_L4(bool sell, MACD macd) {
			string rule="  Regla L4: ";
			string desc="tendencia MACD.Diff";
			if (!ruleL4) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (sell) {
				/*
				La tendencia que buscamos en el diff, en el caso corto es que después de
				unas velas disminuya y en el caso largo, que aumente. ¿podríamos definir el
				número de velas? Es decir, no como esta ahora que en el corto sea menor que
				1 o 2 anteriores sino que el corto podamos definir cuantas barras que remos
				al alza previas al giro y cuantas a la baja en el largo, por ejemplo

				Corto

				vela 0, valor 3
				vela 1, valor 4
				vela 2, valor 3
				vela 3, valor 2

				Es decir, 3 asecentes seguidas y la primera que decrece es 4						
				*/
				if (trending_up(macd.Diff,trend_MACD_diff_period,1)) {  ///conducta ascendente durante todas las N barras anteriores a la 0
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			else {
				if (trending_down(macd.Diff,trend_MACD_diff_period,1)) {  ///conducta descendente durante todas las N barras anteriores a la 0
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			return false;
		}

		public bool rule_L5(bool sell, MACD macd) {
			string rule="  Regla L5: ";
			string desc="MACD.Diff gira";
			if (!ruleL5) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (sell) {
				//5) Macd (diff) vela 0 por debajo de Macd (diff) vela 1
				if (macd.Diff[0] < macd.Diff[1]) { //Que la diferencia entre las dos medias del macd (diff) de la vela 0 sea MENOR que la de la vela 1 o que las dos velas anteriores
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			else {
				//5) Macd (diff) vela 0 por encima de Macd (diff) vela 1
				if (macd.Diff[0] > macd.Diff[1]) { //Que la diferencia entre las dos medias del macd (diff) de la vela 0 sea MENOR que la de la vela 1 o que las dos velas anteriores
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			return false;
		}

		public bool rule_L6(bool sell, RSI rsi) {
			string rule="  Regla L6: ";
			string desc="RSI rebasa umbral";
			if (!ruleL6) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (sell) {
				//que en la vela 0, 1 o 2 tenga mas valor que el parámetro
				if (MAX(rsi,L6_RSI_velas)[0] > RSI_umbral_venta) {
					Print(rule+"[si] " + desc);
					return true;
				}
			}
			else {
				//que en la vela 0, 1 o 2 tenga menos valor que el parámetro
				if (MIN(rsi,L6_RSI_velas)[0] < RSI_umbral_compra) {
					Print(rule+"[si] " + desc);
					return true;
				}
			}
			Print(rule+"[no] " + desc);
			return false;
		}

		public bool rule_L7(bool sell, StochRSI storsi) {
			string rule="  Regla L7: ";
			string desc="STORSI gira desde extremo";
			if (!ruleL7) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (sell) {
				if (storsi[1]==1 && storsi[1]>storsi[0]) {
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			else {
				if (storsi[1]==0 && storsi[1]<storsi[0]) {
					Print(rule+"[si] " + desc);
					return true;
				}
				Print(rule+"[no] " + desc);
			}
			return false;
		}

		public bool rule_L8(ADX adx) {
			string rule="  Regla L8: ";
			string desc="ADX por encima de umbral";
			if (!ruleL8) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (adx[0]>=ADX_umbral_minimo) {
				Print(rule+"[si] " + desc);
				return true;

			}
			Print(rule+"[no] " + desc);
			return false;
		}

		public bool rule_L9(bool sell, Stochastics stoB) {
			string rule="  Regla L9: ";
			string desc="Estocastico B rebasa umbral";
			if (!ruleL9) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			DataSeries S=stoB.D;
			if (linea_estocasticoB==LineaEstocastico.K) {
				S=stoB.K;
				if (CurrentBars[1] <= estocasticoB_K) {			
					Print(rule+"[no] "+desc+". No tiene suficientes barras ("+CurrentBars[1]+"/"+estocasticoA_K+")");
					return false;
				}
			}
			else {
				if (CurrentBars[1] <= estocasticoB_D) {			
					Print(rule+"[no] "+desc+". No tiene suficientes barras ("+CurrentBars[1]+"/"+estocasticoA_D+")");
					return false;
				}
			}

			if (sell) {
				desc+=" sto5 > "+estocasticoB_umbral_venta;
				if (S[0]>estocasticoB_umbral_venta) {  
					Print(rule+"[si] " + desc);
					return true; 
				}
				Print(rule+"[no] " + desc);
			}
			else {
				desc+=" sto5 < "+estocasticoB_umbral_compra;
				if (S[0]<estocasticoB_umbral_compra) {  
					Print(rule+"[si] " + desc);
					return true;  
				}
				Print(rule+"[no] " + desc);
			}
			return false;
		}
		public bool rule_L9(bool sell, Stochastics stoB1, Stochastics stoB2, Stochastics stoB3) {
			bool b1=rule_L9(sell,stoB1);
			if (!b1) return false;
			if (L9_num_estocasticos<2) return b1;
			bool b2=rule_L9(sell,stoB2);
			if (!b2) return false;
			if (L9_num_estocasticos<3) return b2;
			bool b3=rule_L9(sell,stoB3);
			return b3;
		}

		public bool luis_rules(bool mode, Stochastics stoA, MACD macd, RSI rsi, StochRSI storsi, ADX adx, Stochastics stoB1, Stochastics stoB2, Stochastics stoB3) {
			int n=0;
			if (rule_L1(mode,stoA)) n++;
			if (rule_L2(mode,stoA)) n++;
			if (rule_L3(mode,macd)) n++;
			if (rule_L4(mode,macd)) n++;
			if (rule_L5(mode,macd)) n++;
			if (rule_L6(mode,rsi)) n++;
			if (rule_L7(mode,storsi)) n++;
			if (rule_L8(adx)) n++;
			if (rule_L9(mode,stoB1,stoB2,stoB3)) n++;
			return n==9; //all rules true 
		}



		public decision get_decision_luis(Stochastics stoA, MACD macd, RSI rsi, StochRSI storsi, ADX adx, Stochastics stoB1, Stochastics stoB2, Stochastics stoB3) {
			decision ret;
			ret.sell=false;
			ret.buy=false;

//				Print("macd="+macd[0]);	
//			Print("Stochastics D="+sto.D[0]);

			//Corto:
			Print("Corto:");
			if (luis_rules(true,stoA,macd,rsi,storsi,adx,stoB1,stoB2,stoB3)) {
				ret.sell=true;
			}
			Print("Largo:");
			if (luis_rules(false,stoA,macd,rsi,storsi,adx,stoB1,stoB2,stoB3)) {
				ret.buy=true;
			}


			return ret;
		}
		
		
		//-------------------------------------------------------------------------------------
		//-------------------------------------------------------------------------------------
		//-------------------------------------------------------------------------------------
		
		
		
		public enum trend {
			trend_none,
			trend_bearish,
			trend_bullish,
		}
		public trend detect_trend() {
			if (bullish()) return trend.trend_bullish;
			if (bearish()) return trend.trend_bearish;
			return trend.trend_none;
		}

		public bool bearish() {  //no tiene en cuenta la ultima vela, para la detecion de giros
			// v1 sea el min de las penultimas N velas
			double minvalue=MIN(period)[1];
			if (Input[1]<=minvalue) return true;
			return false;
		}
		public bool bullish() {  //no tiene en cuenta la ultima vela, para la detecion de giros
			double maxvalue=MAX(period)[1];
			if (Input[1]>=maxvalue) return true;
			return false;
		}
		public bool is_hammer_up() {
			double length=Highs[0][0]-Lows[0][0];
			double bottom=(Opens[0][0]>Closes[0][0]?Closes[0][0]:Opens[0][0]);
			double topbodylength=Highs[0][0]-bottom;
			double wick=length-topbodylength;
			double percent=wick/length;
			if (percent>wick_length) return true;
			return false;
		}
		public bool is_hammer_down() {
			double length=Highs[0][0]-Lows[0][0];
			double top=(Opens[0][0]<Closes[0][0]?Closes[0][0]:Opens[0][0]);
			double bottombodylength=top-Lows[0][0];
			double wick=length-bottombodylength;
			double percent=wick/length;
			if (percent>wick_length) return true;
			return false;
		}
		
		//public bool is_intentional_bar() { //sin mecha, volumen alto
			
		//}
		


		public bool rule_V2_4(trend t) {
			string rule="  Regla V2.4: ";
			string desc="Martillo de giro";
			if (!ruleV2_4) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (t==trend.trend_bullish) {
				if (is_hammer_down()) {  
					Print(rule+"[si] "+desc);
					return true;
				}
			}
			if (t==trend.trend_bearish) {
				if (is_hammer_up()) {  
					Print(rule+"[si] "+desc);
					return true;
				}
			}
			Print(rule+"[no] "+desc);
			return false;
		}

		public bool rule_V2_3(trend t) {
			string rule="  Regla V2.3: ";
			string desc="Contrario a la tendencia";
			if (!ruleV2_3) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (t==trend.trend_bullish) {
				if (Opens[0][0]>Closes[0][0]) {   //red(down) candle
					Print(rule+"[si] "+desc);
					return true;
				}
			}
			if (t==trend.trend_bearish) {
				if (Opens[0][0]<Closes[0][0]) {  //green(up) candle
					Print(rule+"[si] "+desc);
					return true;
				}
			}
			Print(rule+"[no] "+desc);
			return false;
		}

		public bool rule_V2_2() {
			string rule="  Regla V2.2: ";
			string desc="Doji";
			if (!ruleV2_2) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (Opens[0][0]==Closes[0][0]) {
				Print(rule+"[si] "+desc);
				return true;
			}
			Print(rule+"[no] "+desc);
			return false;
		}

		public bool rule_V2_1(trend t) {
			string rule="  Regla V2.1: ";
			string desc="Hay definida una tendencia";
			if (!ruleV2_2) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			if (t==trend.trend_bullish || t==trend.trend_bearish) {
				Print(rule+"[si] "+desc);
				return true;
			}
			Print(rule+"[no] "+desc);
			return false;
		}

/*
		public bool is_turn(trend t) {
			if (!is_trend(t)) return false;
			Print("Trend");
			if (is_doji()) {
				Print("doji");
				return true;
			}
			if (is_against_trend(t)) {
				Print("against trend");
				return true;
			}
			if (is_hammer_turn(t)) {
				Print("hammer turn");
				return true;
			}
			return false;
		}
*/
		public bool rule_V2(trend t) {
			string rule="  Regla V2: ";
			string desc="Giro";
			if (!ruleV2) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			Print("");
			bool t1=rule_V2_1(t); ///is trend
			bool t2=rule_V2_2();  //doji
			bool t3=rule_V2_3(t);  //against trend
			bool t4=rule_V2_4(t);  //hammer turn
			Print("");

			bool r1=(t1 && t2);  //trend+doji
			bool r2=(t1 && t3);  //trend+against trend
			bool r3=(t1 && t4);  //trend+hammer turn

			bool r=r1 || r2 || r3;

			if (r) {
				Print(rule+"[si] "+desc);
				return true;
			}
			Print(rule+"[no] "+desc);
			return false;
		}

		public bool rule_V1() {
			string rule="  Regla V1: ";
			string desc="Pico de volumen";
			if (!ruleV1) {
				Print(rule+"[si] "+desc+" [filtro desactivado]");
				return true;
			}

			// queremos que el volumen sea mayor todos los N anteriores 
			if (VOL(BarsArray[0])[0]<MAX(VOL(BarsArray[0]),period)[0]) {
				Print(rule+"[si] "+desc);
				return true;
			}
			Print(rule+"[no] "+desc);
			return false;
		}


		public bool vicente_rules(trend t) {
			bool t1=rule_V1(); ///Pico de volumen
			bool t2=rule_V2(t); ///Giro

			bool r1=t1 && t2;

			if (r1) {
				return true;
			}			
			return false;
		}

		//-------------------------------------------------------------------------------------vicente
		public decision get_decision_vicente() {
			decision ret;
			ret.sell=false;
			ret.buy=false;

			trend trend_=detect_trend();

			//Corto:
			if (vicente_rules(trend_)) {
				if (trend_==trend.trend_bearish) ret.sell=true;
				if (trend_==trend.trend_bullish) ret.buy=true;
			}
			
			return ret;
		}


		private int have_contracts=0;  ///+ve long contracts, -ve=sell contracts


		///plot
		private int nlon=0; //+ve , per loop count of long contracts  
		private int nshort=0; //-ve , per loop count of short contracts  

		public bool enter_long(int contratos) { ///risk+hedge
			if (contratos<0) { ///
				Print("Error: No deberia ser negativo contratos");
				return false;
			}
			if (contratos==0) { ///acepta 0 contratos, debe devolver true
				return true;
			}
			if (contratos>3) {
				Print("Error: No deberia ejecutar mas de 3 contratos");
				return false;
			}								

			Print("SIM - ejecutariamos ahora una orden de compra de "+contratos+" contratos");
			have_contracts+=contratos;
			nlon+=contratos;

			return true;  //exito .
		}
		public bool enter_short(int contratos) { ///risk+hedge
			if (contratos<0) { ///
				Print("Error: No deberia ser negativo contratos");
				return false;
			}
			if (contratos==0) { ///acepta 0 contratos, debe devolver true
				return true;
			}
			if (contratos>3) {
				Print("Error: No deberia ejecutar mas de 3 contratos");
				return false;
			}								
			Print("SIM - ejecutariamos ahora una orden de venta de "+contratos+" contratos");
			have_contracts-=contratos;
			nshort-=contratos;
			return true;  //exito
		}


		private double risk_price=0;
		private double stop_loss_price=0;
		private double stop_profit_price=0;
		private DateTime time_enter;
		

		public bool risk(Stochastics stoA, MACD macd, RSI rsi, StochRSI storsi, ADX adx, Stochastics stoB1, Stochastics stoB2, Stochastics stoB3) {
			Print("");
			Print(">>>risk");
			if (have_contracts!=0) {
				Print("Estamos dentro de una operacion de riesgo");
				return false;
			}

			decision d;
			d.buy=d.sell=false;

			//RISK
			if (strategy==Estrategia.Vicente) d=get_decision_vicente();
			if (strategy==Estrategia.Luis) d=get_decision_luis(stoA,macd,rsi,storsi,adx,stoB1,stoB2,stoB3);
			
			if (d.sell || d.buy) {

				string esp="";
				if (strategy==Estrategia.Luis) {
					/*
					Hay un indicador que se llama ATR. Tendrías que hacer
					una nueva condición que solo se da cuando se produzca una entrada a corto o
					a largo de manera que cuando la "pinte" en la pantalla ponga el valor que
					el ATR tiene en ese momento por un parámetro que deberíamos seleccionar en
					la configuración de nuestro indicador y que va desde 1 a 4. Si el parámetro
					es 2 y el atr en el momento de la entrada a corto, por ejemplo, es 0,5.. lo
					multiplicaría por el parámetro y.. debería pintar "Corto? 2020,25 - Esp
					1,00"				
					*/
					ATR atr=ATR(ATR_period);
					double v=Math.Round(ATR_amplificacion*atr[0],2);
					esp=" - Esp "+v;
				}

				if (d.sell && d.buy) {
					Print("wrong decision, cannot both buy and sell");
					d.buy=false;
				}
				int contratos=enter_contratos;
				if (d.sell) {
					if (enter_short(contratos)) {
						Print("  >>Entrado Corto");
						DrawArrowDown("arrowsell"+Times[0][0],true,Times[0][0],Highs[0][0]+2*TickSize,Color.LightPink);
						DrawText("textsell"+Times[0][0],true,"Entra "+contratos+"x"+Closes[0][0].ToString()+esp+"  .", Times[0][0], Highs[0][0]+3*TickSize,0,Color.Black,textfont,StringAlignment.Far,Color.LightPink,Color.LightPink,8);
						risk_price=Closes[0][0];
						//price should go downwards
						stop_loss_price=risk_price+stop_loss;   //+3
						stop_profit_price=risk_price-stop_profit; // -1
						time_enter=Times[0][0];
						ciclo=0;
						num_entradas+=contratos;
					}
				}
				if (d.buy) {
					if (enter_long(contratos)) {
						Print("  >>Entrando Largo");
						DrawArrowUp("arrowbuy"+Times[0][0],true,Times[0][0],Lows[0][0]-2*TickSize,Color.LightBlue);
						DrawText("textbuy"+Times[0][0],true,"Entra "+contratos+"x"+Closes[0][0].ToString()+esp+"  .", Times[0][0], Lows[0][0]-3*TickSize,0,Color.Black,textfont,StringAlignment.Far,Color.LightBlue,Color.LightBlue,8);
						risk_price=Closes[0][0];
						//price should go upwards
						stop_profit_price=risk_price+stop_profit;  //+1
						stop_loss_price=risk_price-stop_loss;  //-3
						time_enter=Times[0][0];
						ciclo=0;
						num_entradas+=contratos;
					}
				}
			}

			return true;
		}
	
		public bool exit_long(int contratos, double price) {
			if (enter_long(contratos)) {
				double current_price=price; //Input[0];
				double usd=Math.Round(contratos*(risk_price-current_price)*50,2);

				Print("  >>Sale Largo con pl $"+usd);
				string msg="";
				msg="Sale "+contratos+"x"+current_price+" ("+usd+" USD)";

				Color col=Color.Red;
				Color dcol=Color.Black;
				if (usd>=0) {
					col=Color.Green;
					dcol=Color.LightGreen;
					num_contratos_ganadores+=contratos;
				}
				else {
					num_contratos_perdedores+=contratos;
				}
				pl+=usd;

				DrawDiamond("bdiamondbuy"+Times[0][0],true,Times[0][0],Highs[0][0]+2*TickSize,dcol);
				DrawText("textsalebuy"+Times[0][0],true,msg+"  .", Times[0][0], Highs[0][0]+3*TickSize,0,col,textfont,StringAlignment.Far,Color.LightBlue,Color.LightBlue,8);
				return true;
			}
			return false;
		}

		public bool exit_short(int contratos, double price) {
			if (enter_short(contratos)) {
				double current_price=price; //Input[0];
				double usd=Math.Round(contratos*(current_price-risk_price)*50,2);
				Print("  >>Sale Corto con pl $"+usd);

				string msg="";

				Color col=Color.Red;
				Color dcol=Color.Black;
				if (usd>=0) {
					col=Color.Green;
					dcol=Color.LightGreen;
					num_contratos_ganadores+=contratos;
					msg="Sale "+contratos+"x"+current_price+" (+"+usd+" USD)";
				}
				else {
					num_contratos_perdedores+=contratos;
					msg="Sale "+contratos+"x"+current_price+" ("+usd+" USD)";
				}
				pl+=usd;

				DrawDiamond("bdiamondsell"+Times[0][0],true,Times[0][0],Highs[0][0]+2*TickSize,dcol);
				DrawText("textsalesell"+Times[0][0],true,msg+"  .", Times[0][0], Highs[0][0]+3*TickSize,0,col,textfont,StringAlignment.Far,Color.LightPink,Color.LightPink,8);
				return true;
			}
			return false;
		}

		public bool check_condicion_salida(CondicionSalida cs,bool enteredshort) {
			if (cs==CondicionSalida.stop_profit_o_estocastico_cruza_nivel) {
				if (check_condicion_salida(CondicionSalida.stop_profit,enteredshort)) {
					return true;
				}
				if (check_condicion_salida(CondicionSalida.estocastico_cruza_nivel,enteredshort)) {
					return true;
				}
				return false;
			}

			double current_price=Closes[0][0];

			if (enteredshort) {
				if (cs==CondicionSalida.stop_profit) {
					if (Lows[0][0]<stop_profit_price) { 
						//Si el precio avanza un punto(stop_profit 1) da mensaje de ganancia en la pantalla
						// (un diamante verde con el literal "+ 1 *50 USD")
						Print ("Crossed Stop-Profit level");
						int contratos=1;
						if (exit_long(contratos,stop_profit_price)) {
							stop_loss_price-=trailing_step;  //Automáticamente MOVEMOS el valor de precio del Stop Loss al punto de entrada de la operación (trailling Stop)
							stop_profit_price-=trailing_step;  //movemos tb el stop_profit
							Print("trailing stop-loss se mueve a " + stop_loss_price);
							return true;

						}
					}
				}
				else if (cs==CondicionSalida.estocastico_cruza_nivel) {
					//Si el precio sin embargo, se sigue moviendo en la dirección esperada
					//y toca el nivel contrario del Estocástico da mensaje de ganancia en la
					//pantalla (un diamante verde con el literal
					//	  "+ 1 * (precio de entrada - precio en el momento de tocar estocástico contrario * 50 USD")
					//DataSeries de 1 minuto
					Stochastics stoA=Stochastics(BarsArray[0],estocasticoA_D,estocasticoA_K,estocasticoA_smooth);
					DataSeries S=stoA.D;
					if (linea_estocasticoA==LineaEstocastico.K) {
						S=stoA.K;
					}
					if (S[0]<estocasticoA_umbral_compra) {  
						Print ("Crossed Stochatics opposite level");
						int contratos=1;
						if (exit_long(contratos,current_price)) {
							return true;
						}
					}
				}
				else if (cs==CondicionSalida.estocastico_gira) {
					//Si el precio se gira y vuelve a cortar el nivel contrario del
					//estocástico pero de vuelta da mensaje de ganancia en la pantalla (un
					//diamante verde con el literal "+ 1 * (precio de entrada - precio en el momento de tocar estocástico contrario de vuelta * 50 USD")
					Stochastics stoA=Stochastics(BarsArray[0],estocasticoA_D,estocasticoA_K,estocasticoA_smooth);
					DataSeries S=stoA.D;
					if (linea_estocasticoA==LineaEstocastico.K) {
						S=stoA.K;
					}
					if (S[1]<=estocasticoA_umbral_compra) {  
						Print ("was in the outer side");
						if (S[0]>estocasticoA_umbral_compra) {  
							Print ("Crossed back Stochatics opposite level");
							int contratos=1;
							if (exit_long(contratos,current_price)) {
								return true;
							}
						}
					}
				}

			}
			else {
				if (cs==CondicionSalida.stop_profit) {
					if (Highs[0][0]>stop_profit_price) { 
						//Si el precio avanza un punto(stop_profit 1) da mensaje de ganancia en la pantalla
						// (un diamante verde con el literal "+ 1 *50 USD")
						Print ("Crossed Stop-Profit level");
						int contratos=1;
						if (exit_short(contratos,stop_profit_price)) {
							stop_loss_price+=trailing_step;  //Automáticamente MOVEMOS el valor de precio del Stop Loss al punto de entrada de la operación (trailling Stop)
							stop_profit_price+=trailing_step;  //movemos tb el stop_profit
							Print("trailing stop-loss se mueve a " + stop_loss_price);
							return true;
						}
					}
				}
				else if (cs==CondicionSalida.estocastico_cruza_nivel) {
					//Si el precio sin embargo, se sigue moviendo en la dirección esperada
					//y toca el nivel contrario del Estocástico da mensaje de ganancia en la
					//pantalla (un diamante verde con el literal
					//	  "+ 1 * (precio de entrada - precio en el momento de tocar estocástico contrario * 50 USD")
					Stochastics stoA=Stochastics(BarsArray[0],estocasticoA_D,estocasticoA_K,estocasticoA_smooth);
					DataSeries S=stoA.D;
					if (linea_estocasticoA==LineaEstocastico.K) {
						S=stoA.K;
					}
					if (S[0]>estocasticoA_umbral_venta) { //85
						Print ("Crossed Stochatics opposite level");
						int contratos=1;
						if (exit_short(contratos,current_price)) {
							return true;
						}
					}
				}
				else if (cs==CondicionSalida.estocastico_gira) {
					//Si el precio se gira y vuelve a cortar el nivel contrario del
					//estocástico pero de vuelta da mensaje de ganancia en la pantalla (un
					//diamante verde con el literal "+ 1 * (precio de entrada - precio en el momento de tocar estocástico contrario de vuelta * 50 USD")
					Stochastics stoA=Stochastics(BarsArray[0],estocasticoA_D,estocasticoA_K,estocasticoA_smooth);
					DataSeries S=stoA.D;
					if (linea_estocasticoA==LineaEstocastico.K) {
						S=stoA.K;
					}
					if (S[1]>=estocasticoA_umbral_venta) { //85
						Print ("was in the outer side");
						if (S[0]<estocasticoA_umbral_venta) {  //85 
							Print ("Crossed back Stochatics opposite level");
							int contratos=1;
							if (exit_short(contratos,current_price)) {
								return true;
							}
						}
					}
				}

			}
			return false;
		}


		public bool check_stop_loss(bool entered_short) {
			if (entered_short) {
				if (Highs[0][0]>stop_loss_price) { 
					Print ("Crossed Stop-Loss level");
					int contratos=-have_contracts;
					if (exit_long(contratos,stop_loss_price)) {
						risk_price=0;
						return false;
					}
				}
			}
			else {  ///entered long
				if (Lows[0][0]<stop_loss_price) { 
					Print ("Crossed Stop-Loss level");
					int contratos=have_contracts;
					if (exit_short(contratos,stop_loss_price)) {
						risk_price=0;
						return false;
					}
				}
			}
			return true;
		}

		public bool check_time(bool entered_short) {
			if (ToTime(Times[0][0])>activity_interval_to) {
				Print ("time of the day is >21.50");
				int contratos=-have_contracts;
				double current_price=Closes[0][0];
				if (entered_short) {
					if (exit_long(contratos,current_price)) {
						risk_price=0;
						return false;
					}
				}
				else {  ///entered long
					if (exit_short(contratos,current_price)) {
						risk_price=0;
						return false;
					}
				}
			}
			return true;
		}


		public bool hedge() {

			if (!Historical) {
				RemoveDrawObject("StopLoss");
				RemoveDrawObject("StopProfit");

//			Print( Input[0]);  //1min
//			Print( BarsArray[0][0]); 
//			Print( BarsArray[0].GetClose(0)); 
//			Print( BarsArray[1].GetClose(0)); 
			}
			Print("");
			Print(">>>hedge");
			if (have_contracts==0) {
				Print("nothing to hedge");
				return false; ///nada que cubrir
			}
			double current_price=Closes[0][0];
//			double current_price=Input[0];
			Print ("current_price "+current_price+" stop_loss_price "+stop_loss_price);
			if (!Historical) {
				if (ciclo==0) { //solo en el ciclo 0 el limite esta fijado por el precio, en el ciclo 1 es el estocastico
					DrawLine("StopLoss",true,time_enter,stop_loss_price,Times[0][0],stop_loss_price,Color.Red,DashStyle.Dot,3);
					DrawLine("StopProfit",true,time_enter,stop_profit_price,Times[0][0],stop_profit_price,Color.Green,DashStyle.Dot,3);
				}
				else {
					DrawLine("StopLoss",true,time_enter,stop_loss_price,Times[0][0],stop_loss_price,Color.LightGreen,DashStyle.Dot,3);
				}
			}

			bool entered_short=have_contracts<0;

			if (!check_time(entered_short)) {
			}
			else if (!check_stop_loss(entered_short)) {
			}
			else if (ciclo==0) {
				if (check_condicion_salida(condicion_salida_contrato1,entered_short)) {
						ciclo++;   //aumenta el ciclo gestion contratos
				}
			}
			else if (ciclo==1) {
				if (check_condicion_salida(condicion_salida_contrato2,entered_short)) {
						ciclo++;   //ciclo gestion contratos
				}
			}
			else if (ciclo==2) {
				if (check_condicion_salida(condicion_salida_contrato3,entered_short)) {
						ciclo++;   //ciclo gestion contratos
				}
			}



			return true;

		}
/*
			double current_price=Input[0];
			double diff=current_price-risk_price;
			Print("diff="+diff);
			///diff <0 means price fall, bearish behavior
			///diff >0 means price rise, bullish behavior
			bool profiting=false;

			bool bearish=false;
			if (diff<=0) bearish=true;
			if (bearish) {
				// si bearish and entered short (have_contracts<0) --> profit
				if (have_contracts<0) { ///entered short
					profiting=true;
				}
				// si bearish and entered long (have_contracts>0) --> loss
			}

			bool bullish=false;
			if (diff>=0) bullish=true;
			if (bullish) {
				// si bullish and entered long (have_contracts>0) --> profit
				if (have_contracts>0) { ///entered short
					profiting=true;
				}
				// si bullish and entered short (have_contracts<0) --> loss
			}


			if (!profiting) { //precio no avanza en la dirección esperada
				Print ("Losing");
				if (Math.abs(current_price-stop_loss_price)>stop_loss) { ///parametro stop_loss -> limite de perdidas en precio. i.e. 3 puntos o $150 o 12 ticks
					Print ("Crossed Stop-Loss level");
					if (have_contracts<0) {  ///hay que comprar 
						int contratos=-have_contracts;
						if (enter_long(contratos)) {
							EnterShort.Set(0);
							EnterLong.Set(contratos);
							double loss_usd=Math.Round(contratos*Math.abs(diff)*50),2);
							Print("  >>Sale Largo con losses -$"+loss_usd);
							DrawDiamond("bdiamondbuy"+Time[0],true,Time[0],Low[0]-2*TickSize,Color.Black);
							DrawText("textsalebuy"+Time[0],true,"Loss -"+loss_usd+" USD  .", Time[0], Low[0]-3*TickSize,0,Color.Red,textfont,StringAlignment.Far,Color.LightBlue,Color.LightBlue,8);
							risk_price=0;
						}
					}
					else {
						int contratos=have_contracts;
						if (enter_short(contratos)) {
							EnterShort.Set(-1*contratos);
							EnterLong.Set(0);
							double loss_usd=Math.Round(contratos*Math.abs(diff)*50),2);
							Print("  >>Sale Corto con losses -$"+loss_usd);
							//(un diamante negro por ejemplo con el literal "- 3 * parámetro Stop loss * 50 USD")
							//"- 3 * parámetro Stop loss * 50 USD" - (num_contratos) * stop_loss
							DrawDiamond("bdiamondsell"+Time[0],true,Time[0],High[0]+2*TickSize,Color.Black);
							string msg;
							if (ciclo==0) {
								//Si el precio toca ese stop muestra mensaje de perdida en la pantalla (un diamante negro por ejemplo
							    //con el literal "- 3 * parámetro Stop loss * 50 USD")
								msg="Loss -"+loss_usd+" USD";
							}
							else if (ciclo==1) {
								//Si el precio toca ese "nuevo valor del stop" da mensaje de cierre de stop en la pantalla (un diamante negro por ejemplo
							    // con el literal "Cierre Stop protección: Ganancia total "+ 1 * 50 USD")
								msg="Cierre Stop protección: Ganancia total "+loss_usd+" USD";
							}
							DrawText("textsalesell"+Time[0],true,msg+"  .", Time[0], High[0]+3*TickSize,0,Color.Red,textfont,StringAlignment.Far,Color.LightPink,Color.LightPink,8);
							risk_price=0;
						}
					}
				}
				//Coloca Stop loss a stop_loss puntos del precio de entrada
				//Si el precio toca ese stop muestra mensaje de perdida en la pantalla (un diamante negro por ejemplo con el literal "- 3 * parámetro Stop loss * 50 USD")
			}
			else {
				Print ("Profiting");
				if (ciclo==0) {
					if (Math.abs(stop_profit_price-current_price)>stop_profit) { 
						//Si el precio avanza un punto(stop_profit 1) da mensaje de ganancia en la pantalla
						// (un diamante verde con el literal "+ 1 *50 USD")
						Print ("Crossed Stop-Profit level");
						int contratos=1;
						if (have_contracts<0) {  ///hay que comprar 
							if (enter_long(contratos)) {
								EnterShort.Set(0);
								EnterLong.Set(contratos);
								double profit_usd=Math.Round(contratos*Math.abs(diff)*50),2);
								Print("  >>Sale Largo con profit +$"+profit_usd);
								DrawDiamond("bdiamondbuy"+Time[0],true,Time[0],Low[0]-2*TickSize,Color.Green);
								DrawText("textsalebuy"+Time[0],true," +"+profit_usd+" USD"+"  .", Time[0], Low[0]-3*TickSize,0,Color.Green,textfont,StringAlignment.Far,Color.LightBlue,Color.LightBlue,8);
								stop_loss_price-=trailing_step;  //Automáticamente MOVEMOS el valor de precio del Stop Loss al punto de entrada de la operación (trailling Stop)
								stop_profit_price-=trailing_step;  //movemos tb el stop_profit
								Print("trailing stop-loss se mueve a " + stop_loss_price);
								ciclo++;   //aumenta el ciclo de profit
							}
						}
						else {
							if (enter_short(contratos)) {
								EnterShort.Set(-1*contratos);
								EnterLong.Set(0);
								double profit_usd=Math.Round(contratos*Math.abs(diff)*50),2);
								Print("  >>Sale Corto con profit +$"+profit_usd);
								DrawDiamond("bdiamondsell"+Time[0],true,Time[0],High[0]+2*TickSize,Color.Green);
								DrawText("textsalesell"+Time[0],true,"+"+profit_usd+" USD"+"  .", Time[0], High[0]+3*TickSize,0,Color.Green,textfont,StringAlignment.Far,Color.LightPink,Color.LightPink,8);
								stop_loss_price+=trailing_step;  //Automáticamente MOVEMOS el valor de precio del Stop Loss al punto de entrada de la operación (trailling Stop)
								stop_profit_price+=trailing_step;  //movemos tb el stop_profit
								Print("trailing stop-loss se mueve a " + stop_loss_price);
								ciclo++;   //aumenta el ciclo de profit
							}
						}
					}
				}
				else {  //ciclo 1
					//Si el precio sin embargo, se sigue moviendo en la dirección esperada
					//y toca el nivel contrario del Estocástico da mensaje de ganancia en la
					//pantalla (un diamante verde con el literal
					//	  "+ 1 * (precio de entrada - precio en el momento de tocar estocástico contrario * 50 USD")

				}
			}

		}
*/
		private string lastresettag="";

		
        private bool reset_stats() {
			if (have_contracts!=0) {
				Print("No se ha podido resetear la estadistica");
				return false;
			}
			num_entradas=0;
			num_contratos_ganadores=0;
			num_contratos_perdedores=0;
			pl=0;
			return true;
		}
/*		
		private void test_indicator() {
			if (Historical) return;
			Print("--------------------------------cc");
			Print(BarsArray[0].Count);
			System.IO.StreamWriter file=new System.IO.StreamWriter("C:\\Users\\pedro ayala\\Documents\\NinjaTrader 7\\bin\\Custom\\ExportNinjaScript\\data.txt");
			for (int i=0; i<BarsArray[0].Count-1; ++i) {
//Print("  = "+i );			
				file.WriteLine(BarsArray[0][i]);
//Print("  = "+i+"ok"+ BarsArray[0][i]);			
			}
			file.Close();
			
			//read file
			//
//Print("===a");			
			MACD macd=MACD(BarsArray[0],12,26,9);
//Print("===b");			

			System.IO.StreamWriter macdfile=new System.IO.StreamWriter("C:\\Users\\pedro ayala\\Documents\\NinjaTrader 7\\bin\\Custom\\ExportNinjaScript\\macd.txt");
			for (int i=0; i<macd.Count-1; ++i) {
				macdfile.WriteLine(macd[i]);
			}
			macdfile.Close();

			
			System.IO.StreamReader filee=new System.IO.StreamReader("C:\\no existe"); //da error para que termine
		}
*/

		private void print_indicators(string prefix,Stochastics stoA, MACD macd, RSI rsi, StochRSI storsi, ADX adx, Stochastics stoB) {
			Print(prefix+" MACD_1m fast "+macd[0]);
			Print(prefix+" MACD_1m signal "+macd.Avg[0]);
			Print(prefix+" MACD_1m hist "+macd.Diff[0]);
			Print(prefix+" STOA K "+stoA.K[0]);
			Print(prefix+" STOA D "+stoA.D[0]);
			Print(prefix+" RSI_1m "+rsi[0]);
			Print(prefix+" STORSI_1m fK "+storsi[0]);
			Print(prefix+" STORSI_1m fD n/a");
			Print(prefix+" ADX_1m "+adx[0]);
			if (CurrentBars[1] > estocasticoB_K) {			
				Print(prefix+" STOB K "+stoB.K[0]);
				Print(prefix+" STOB D "+stoB.D[0]);
			}
			else {
				Print(prefix+" STOB sK n/a");
				Print(prefix+" STOB sD n/a");
			}
		}
		private void write_indicators(System.IO.StreamWriter file, string prefix,Stochastics stoA, MACD macd, RSI rsi, StochRSI storsi, ADX adx, Stochastics stoB) {
			file.WriteLine(prefix+" MACD_1m fast "+macd[0]);
			file.WriteLine(prefix+" MACD_1m signal "+macd.Avg[0]);
			file.WriteLine(prefix+" MACD_1m hist "+macd.Diff[0]);
			file.WriteLine(prefix+" STOA K "+stoA.K[0]);
			file.WriteLine(prefix+" STOA D "+stoA.D[0]);
			file.WriteLine(prefix+" RSI_1m "+rsi[0]);
			file.WriteLine(prefix+" STORSI_1m "+storsi[0]);
			file.WriteLine(prefix+" ADX_1m "+adx[0]);
			if (CurrentBars[1] > estocasticoB_K) {			
				file.WriteLine(prefix+" STOB K "+stoB.K[0]);
				file.WriteLine(prefix+" STOB D "+stoB.D[0]);
			}
			else {
				file.WriteLine(prefix+" STOB K n/a");
				file.WriteLine(prefix+" STOB D n/a");
			}
		}

		
		/*
		IND STO_1m sK 64
		IND STO_1m sD 75.5556
		IND MACD_1m fast 1.30944
		IND MACD_1m signal 1.00134
		IND MACD_1m hist 0.308098
		IND RSI_1m 48.7179
		IND STORSI_1m fK 100
		IND STORSI_1m fD 100
		IND ADX_1m 13.3386
		IND STO_5m sK 93.1078
		IND STO_5m sD 93.4492
*/

		private void write_params() {
			using(System.IO.StreamWriter file = new System.IO.StreamWriter(Cbi.Core.UserDataDir.ToString() + "MMOlog.txt",true)) {
			file.WriteLine("----------------------------------principio parametros");
			file.WriteLine("activity_interval_from " + activity_interval_from);
			file.WriteLine("activity_interval_to " + activity_interval_to);
			file.WriteLine("macd_slow_period " + macd_slow_period);
			file.WriteLine("macd_fast_period " + macd_fast_period);
			file.WriteLine("macd_signal_period " + macd_signal_period);
			file.WriteLine("MACD_umbral_venta " + MACD_umbral_venta);
			file.WriteLine("MACD_umbral_compra " + MACD_umbral_compra);
			file.WriteLine("trend_MACD_diff_period " + trend_MACD_diff_period);
			file.WriteLine("estocasticoA_D " + estocasticoA_D);
			file.WriteLine("estocasticoA_K " + estocasticoA_K);
			file.WriteLine("estocasticoA_smooth " + estocasticoA_smooth);
			file.WriteLine("estocasticoA_umbral_venta " + estocasticoA_umbral_venta);
			file.WriteLine("estocasticoA_umbral_compra " + estocasticoA_umbral_compra);
			file.WriteLine("linea_estocasticoA " + linea_estocasticoA);
			file.WriteLine("rsi_period " + rsi_period);
			file.WriteLine("rsi_smooth " + rsi_smooth);
			file.WriteLine("RSI_umbral_venta " + RSI_umbral_venta);
			file.WriteLine("RSI_umbral_compra " + RSI_umbral_compra);
			file.WriteLine("stochasticsrsi_period " + stochasticsrsi_period);
			file.WriteLine("adx_period " + adx_period);
			file.WriteLine("ADX_umbral_minimo " + ADX_umbral_minimo);
			file.WriteLine("estocasticoB_D " + estocasticoB_D);
			file.WriteLine("estocasticoB_K " + estocasticoB_K);
			file.WriteLine("estocasticoB_smooth " + estocasticoB_smooth);
			file.WriteLine("estocasticoB_umbral_venta " + estocasticoB_umbral_venta);
			file.WriteLine("estocasticoB_umbral_compra " + estocasticoB_umbral_compra);
			file.WriteLine("linea_estocasticoB " + linea_estocasticoB);
			file.WriteLine("enter_contratos " + enter_contratos);
			file.WriteLine("stop_loss " + stop_loss);
			file.WriteLine("stop_profit " + stop_profit);
			file.WriteLine("trailing_step " + trailing_step);
			file.WriteLine("condicion_salida_contrato1 " + condicion_salida_contrato1);
			file.WriteLine("condicion_salida_contrato2 " + condicion_salida_contrato2);
			file.WriteLine("condicion_salida_contrato3 " + condicion_salida_contrato3);
			file.WriteLine("ruleL1 " + ruleL1);
			file.WriteLine("ruleL2 " + ruleL2);
			file.WriteLine("ruleL3 " + ruleL3);
			file.WriteLine("ruleL4 " + ruleL4);
			file.WriteLine("ruleL5 " + ruleL5);
			file.WriteLine("ruleL6 " + ruleL6);
			file.WriteLine("ruleL7 " + ruleL7);
			file.WriteLine("ruleL8 " + ruleL8);
			file.WriteLine("ruleL9 " + ruleL9);
			file.WriteLine("----------------------------------fin parametros");
			}			
		}
				
		
        protected override void OnBarUpdate() {
			Print("");
			Print("");
			Print("");
			Print("");
			Print("==============================================================================loop "+Times[0][0]+" currentBar "+CurrentBars[0]);

			System.IO.StreamWriter file=null; 
			if (write_MMOlog) {
				file = new System.IO.StreamWriter(Cbi.Core.UserDataDir.ToString() + "MMOlog.txt",true);
				file.WriteLine("");
				file.WriteLine("");
				file.WriteLine("");
				file.WriteLine("");
				file.WriteLine("==============================================================================loop "+Times[0][0]+" currentBar "+CurrentBars[0]);
			}
		Print("Barras 0: "+(CurrentBars[0]+1)+" "+BarsArray[0].Period);
		Print("Barras B: "+(CurrentBars[1]+1)+" "+BarsArray[1].Period);
		Print("Barras A: "+(CurrentBars[2]+1)+" "+BarsArray[2].Period);

		//dataset de 1 minuto
		Stochastics stoA=Stochastics(BarsArray[2],estocasticoA_D,estocasticoA_K,estocasticoA_smooth);
			
		MACD macd=MACD(BarsArray[0],macd_fast_period,macd_slow_period,macd_signal_period);
		RSI rsi=RSI(BarsArray[0],rsi_period,rsi_smooth);
		StochRSI storsi=StochRSI(BarsArray[0],stochasticsrsi_period);
		ADX adx=ADX(BarsArray[0],adx_period);

		//dataset de 5 minutos
		Stochastics stoB1=Stochastics(BarsArray[1],estocasticoB_D,estocasticoB_K,estocasticoB_smooth);
		Stochastics stoB2=Stochastics(BarsArray[3],estocasticoB_D,estocasticoB_K,estocasticoB_smooth);
		Stochastics stoB3=Stochastics(BarsArray[4],estocasticoB_D,estocasticoB_K,estocasticoB_smooth);

//***********************************************************
/*
		MIN mn1=MIN(Lows[0], stochastics_K);
		MAX mx1=MAX(Highs[0], stochastics_K);
			Print("Instrumentacion STOCASTICO");
			Print("MIN(Low, PeriodK)[0]="+mn1[0]);
			Print("MAX(High, PeriodK)[0]="+mx1[0]);
			double num=Closes[0][0] - mn1[0];
			double den=mx1[0] - mn1[0];
		Print("Num:"+num);
		Print("Den:"+den);
			double fastK=0;
            if (den.Compare(0, 0.000000000001) == 0) {
			}
            else {
                fastK=Math.Min(100, Math.Max(0, 100 * num / den));
            }    
			Print ("fastK="+fastK);
*/			
			/*
			nom.Set(Close[0] - MIN(Low, PeriodK)[0]);
            den.Set(MAX(High, PeriodK)[0] - MIN(Low, PeriodK)[0]);

            if (den[0].Compare(0, 0.000000000001) == 0)
                fastK.Set(CurrentBar == 0 ? 50 : fastK[1]);
            else
                fastK.Set(Math.Min(100, Math.Max(0, 100 * nom[0] / den[0])));

            // Slow %K == Fast %D
            K.Set(SMA(fastK, Smooth)[0]);
            D.Set(SMA(K, PeriodD)[0]);
			*/
//***********************************************************
			
			
		
			
			
		Print("IND time "+Times[0][0].TimeOfDay.TotalMinutes);
		Print("IND price "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]);
		Print("IND QUOTE "+Times[0][0]+" "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]+" "+VOL(BarsArray[0])[0]);
		Print("-------------------------------------indicadores");
		print_indicators("IND ",stoA,macd,rsi,storsi,adx,stoB1);
		Print("-----------------------------------/-indicadores");
	
		if (write_MMOlog) {
			file.WriteLine("Barras A: "+(CurrentBars[2]+1)+" "+BarsArray[2].Period);
			file.WriteLine("Barras B: "+(CurrentBars[1]+1)+" "+BarsArray[1].Period);
			file.WriteLine("IND time "+Times[0][0].TimeOfDay.TotalMinutes);
			file.WriteLine("IND price "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]);
			file.WriteLine("IND QUOTE "+Times[0][0]+" "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]+" "+VOL(BarsArray[0])[0]);
			file.WriteLine("-------------------------------------indicadores");
			write_indicators(file,"IND ",stoA,macd,rsi,storsi,adx,stoB1);
			file.WriteLine("-----------------------------------/-indicadores");

/*
			file.WriteLine("ISTO Instrumentacion STOCASTICO");
			file.WriteLine("ISTO time "+Times[0][0].TimeOfDay.TotalMinutes);
			file.WriteLine("ISTO price "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]);
			file.WriteLine("ISTO PeriodK=" + sto.PeriodK);
			file.WriteLine("ISTO MIN(Low, PeriodK)[0]="+mn1[0]);
			file.WriteLine("ISTO MAX(High, PeriodK)[0]="+mx1[0]);
			file.WriteLine("ISTO Num:"+num);
			file.WriteLine("ISTO Den:"+den);
			file.WriteLine("ISTO fastK="+fastK);


			file.WriteLine("IRSI Instrumentacion RSI");
			file.WriteLine("IRSI time "+Times[0][0].TimeOfDay.TotalMinutes);
			file.WriteLine("IRSI price "+Opens[0][0]+" "+Highs[0][0]+" "+Lows[0][0]+" "+Closes[0][0]);
			file.WriteLine("IRSI Period=" + drsi.Period);
			file.WriteLine("IRSI Smooth=" + drsi.Smooth);
			file.WriteLine("IRSI down=" + drsi.down[0]);
			file.WriteLine("IRSI up=" + drsi.up[0]);
			file.WriteLine("IRSI AvgUp[0]=" + drsi.avgUp[0]);
			file.WriteLine("IRSI AvgDown[0]=" + drsi.avgDown[0]);
			file.WriteLine("IRSI Avg[0]=" + drsi.Avg[0]);
			file.WriteLine("IRSI Avg[0]=" + drsi.Avg[0]);
			file.WriteLine("IRSI rsi[0]=" + drsi[0]);
*/
		}			
			
			//file.WriteLine("IRSI down[0]="+rsi.down[0]);
			//file.WriteLine("IRSI up[0]="+rsi.up[0]);
			//file.WriteLine("IRSI AvgUp[0]="+rsi.avgUp[0]);
			//file.WriteLine("IRSI AvgDown[0]="+rsi.avgDown[0]);
		
//		return;	


			if (CurrentBars[0]<period) {
					Print("pocas barras");
					if (write_MMOlog) {
						file.WriteLine("pocas barras");
					}
					EnterShort.Set(0);
					EnterLong.Set(0);
				return;
			}



			//if (CurrentBar>1) DrawRay("Ray",1,Close[0],0,Close[0],Color.Blue);
			
			Print("Have contracts="+have_contracts);
			if (write_MMOlog) {
				file.WriteLine("Have contracts="+have_contracts);
			}

			nlon=0;
			nshort=0;

			bool hook=true;
			//Intervalo de actividad
			if (ToTime(Times[0][0])<activity_interval_from || ToTime(Times[0][0])>=activity_interval_to) {
				Print("Hook is disabled, fuera de intervalo de actividad");
				if (write_MMOlog) {
					file.WriteLine("Hook is disabled, fuera de intervalo de actividad");
				}
				hook=false;
				
			}

			
			if (hook) {
				risk(stoA,macd,rsi,storsi,adx,stoB1,stoB2,stoB3);
			}
			hedge();
			
			EnterLong.Set(nlon);
			EnterShort.Set(nshort);
		
			double completados=num_contratos_ganadores+num_contratos_perdedores;
			double compl_percent_win=0;
			if (completados>0) {
				compl_percent_win=Math.Round(100.0*(num_contratos_ganadores/completados),2);
			}
			double compl_percent_lose=0;
			if (completados>0) {
				compl_percent_lose=Math.Round(100.0*(num_contratos_perdedores/completados),2);
			}
			
			Print("");
			Print("Estadistica");
			Print("-----------");
			Print("Contratos de Entrada: "+num_entradas);
			Print("Contratos de Salida completados: "+completados);
			Print("  ganadores: "+num_contratos_ganadores+" ("+compl_percent_win+"%)");
			Print("  perdedores: "+num_contratos_perdedores+" ("+compl_percent_lose+"%)");
			Print("Contratos de Salida pendientes: "+(num_entradas-(num_contratos_ganadores+num_contratos_perdedores)));
			Print("Ganancia / pérdida total: $ "+pl);
			
			Color col=Color.Green;
			if (pl<0) col=Color.Red;
			
			int barsago=30;
			if (CurrentBars[0]>barsago) {
				string curtag="dailylabel"+Times[0][barsago].Date; //+"_"+Time[barsago].Hour;
				if (curtag!=lastresettag) {
					if (reset_stats()) {
						lastresettag=curtag;
					}
				}
				string text;
				text="dia "+Times[0][barsago].Date+"\n";
				text=text+"Contratos de Entrada: "+num_entradas+"\n";
				text=text+"Contratos de Salida completados: "+completados+"\n";
				text=text+"    ganadores: "+num_contratos_ganadores+" ("+compl_percent_win+"%)"+"\n";
				text=text+"   perdedores: "+num_contratos_perdedores+" ("+compl_percent_lose+"%)"+"\n";
				text=text+"Ganancia/Perdida total: $"+pl+"\n";
				DrawText(curtag,true,text, Times[0][barsago], Highs[0][barsago]+15*TickSize,0,col,textfont,StringAlignment.Near,Color.Black,Color.White,7);
			}
			//DrawText("textsell"+Time[0],true,"Entra "+contratos+"x"+Close[0].ToString()+esp+"  .", Time[0], High[0]+3*TickSize,0,Color.Black,textfont,StringAlignment.Far,Color.LightPink,Color.LightPink,8);
									
		}		

/*
			class bet {
				type _type; //sell or buy
				int matched;  //shares contracted
				int unmatched;  //shares waiting
				double _price; 

				double get_PL() const {
					double sign=1;
					if (type==sell) sign=-1;
					return sign*matched*_price;
				}
			}

			class bets: public list<bet> {
				int volume;
				double _price; //enter price

				double get_PL() const {
					double pl=0;
					for (i=begin(); i!=end(); ++i) {
						const bet& b=*i;
						pl+=b.get_PL();
					}
					return pl;
				}
			}
*/

      //  [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
      //  [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove


//---------------------------------------------rules Vicente


		[Description("Pico de Volumen. Positivo cuando el volumen de la barra actual es mayor todos los N anteriores, definido por el parametro 'Numero de barras' ")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 1")]
		public bool RuleV1 {
			get { return ruleV1; }
			set { ruleV1 = value; }
		}

		[Description("Deteccion de Giro. Es positivo cuando 2.1 es positivo y ademas al menos una de [2.2, 2.3, 2.4] es positiva.")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 2")]
		public bool RuleV2 {
			get { return ruleV2; }
			set { ruleV2 = value; }
		}

		[Description("Deteccion de tendencia, positivo si hay definida una tendencia")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 2.1")]
		public bool RuleV2_1 {
			get { return ruleV2_1; }
			set { ruleV2_1 = value; }
		}

		[Description("Deteccion de Doji. Positivo cuando la barra actual es una cruz o doji.")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 2.2")]
		public bool RuleV2_2 {
			get { return ruleV2_2; }
			set { ruleV2_2 = value; }
		}

		[Description("Deteccion de movimiento contra corriente. Positivo cuando la barra actual rompe la tendencia.")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 2.3")]
		public bool RuleV2_3 {
			get { return ruleV2_3; }
			set { ruleV2_3 = value; }
		}

		[Description("Deteccion de martillo de giro. Si la tendencia es descendente esta condicion es positiva cuando se detecta una figura de martillo, si es ascendente la condicion es verdadera si se detecta una figura martillo invertido.")]
		[GridCategory("Parametros/Vicente/Rules")] 
		[Gui.Design.DisplayName("Filtro 2.4")]
		public bool RuleV2_4 {
			get { return ruleV2_4; }
			set { ruleV2_4 = value; }
		}


//---------------------------------------------rules Luis


		[Description("Para entrar corto/largo, Estocastico A (linea seleccionada) esta por encima/debajo del umbral definido por EstocasticoA-'Umbral venta/compra'")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 1")]
		public bool RuleL1 {
			get { return ruleL1; }
			set { ruleL1 = value; }
		}

		[Description("Para entrar corto/largo, Estocastico A (linea seleccionada) decrece/crece con respecto a la vela anterior.")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 2")]
		public bool RuleL2 {
			get { return ruleL2; }
			set { ruleL2 = value; }
		}

		[Description("Para entrar corto/largo, MACD cruza el nivel definido por 'MACD-Umbral venta/compra'")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 3")]
		public bool RuleL3 {
			get { return ruleL3; }
			set { ruleL3 = value; }
		}

		[Description("Para entrar corto/largo, La linea MACD.Diff debe haber estado creciendo/decreciendo durante el numero de periodos definidos por 'Barras Trend Diff'")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 4")]
		public bool RuleL4 {
			get { return ruleL4; }
			set { ruleL4 = value; }
		}

		[Description("Para entrar corto/largo,La linea MACD.Diff debe presentar una bajada/subida con respecto a la ultima vela.")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 5")]
		public bool RuleL5 {
			get { return ruleL5; }
			set { ruleL5 = value; }
		}


		[Description("Para entrar corto/largo, En la vela 0 tenga mas/menos valor que el parametro 'RSI param'.")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 6")]
		public bool RuleL6 {
			get { return ruleL6; }
			set { ruleL6 = value; }
		}

		[Description("Para entrar corto/largo, Vela 1 tenga valor 1/0 y la vela 0 tenga menos/mas valor.")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 7")]
		public bool RuleL7 {
			get { return ruleL7; }
			set { ruleL7 = value; }
		}

		[Description("Para entrar es necesario que ADX tenga un valor superior al 'umbral minimo'.")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 8")]
		public bool RuleL8 {
			get { return ruleL8; }
			set { ruleL8 = value; }
		}

		[Description("Para entrar corto/largo, Estocastico B (linea seleccionada) esta por encima/debajo del umbral definido por Estocastico B-'Umbral venta/compra'")]
		[GridCategory("Parametros/Luis/Rules")] 
		[Gui.Design.DisplayName("Filtro 9")]
		public bool RuleL9 {
			get { return ruleL9; }
			set { ruleL9 = value; }
		}


		[Description("MACD, slow EMA of price. periods")]
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Slow Period")]
		public int MACD_Slow {
			get { return macd_slow_period; }
			set { macd_slow_period = Math.Max(1, value); }
		}
	
		[Description("MACD, fast EMA of price. periods")]
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Fast Period")]
		public int MACD_Fast {
			get { return macd_fast_period; }
			set { macd_fast_period = Math.Max(1, value); }
		}
	
		[Description("MACD, EMA of MACD. periods")]
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Signal Period")]
		public int MACD_Smooth {
			get { return macd_signal_period; }
			set { macd_signal_period = Math.Max(1, value); }
		}

		[Description("MACD, umbral venta. [0,6]")]  //// 3) Macd por encima de un parámetro (nunca menos de 0, máximo 6)
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Umbral venta")]
		public double MACD_Level_Sell {
			get { return MACD_umbral_venta; }
			set { 
				if (value>6) value=6;
				if (value<0) value=0;
				MACD_umbral_venta = value; 
			}
		}

		[Description("MACD, umbral compra. [0,-6]")]
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Umbral compra")]
		public double MACD_Level_Buy {
			get { return MACD_umbral_compra; }
			set { 
				if (value>0) value=0;
				if (value<-6) value=-6;
				MACD_umbral_compra =value; 
			}
		}

		[Description("Numero de barras usadas en el calculo de la tendencia de la linea MACD.Diff. Un valor de 3 produciria una chequeo del diff desde la barra 3 hasta la 1 (3 barras) que ha de verificar conducta ascendente/descendente, quedando fuera del calculo la barra 0, donde se ha de verificar ademas un descenso/ascenso para poder entrar corto/largo.")]
		[GridCategory("Parametros/Luis/MACD")] 
		[Gui.Design.DisplayName("Barras Trend Diff")]
		public int Trend_MACD_diff_period {
			get { return trend_MACD_diff_period; }
			set { trend_MACD_diff_period =Math.Max(1, value); }
		}
		
		
		[Description("Estocastico A, D")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("D")]
		public int StochasticsA_D {
			get { return estocasticoA_D; }
			set { estocasticoA_D = Math.Max(1, value); }
		}
		[Description("Estocastico A, K")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("K")]
		public int StochasticsA_K {
			get { return estocasticoA_K; }
			set { estocasticoA_K = Math.Max(1, value); }
		}
		[Description("Estocastico A, Smooth")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("smooth")]
		public int StochasticsA_Smooth {
			get { return estocasticoA_smooth; }
			set { estocasticoA_smooth = Math.Max(1, value); }
		}

		[Description("Nivel superior del estocastico A. [80-100]. Para detectar el corte del estocastico con el umbral de venta se utilizan el numero de barras anteriores indicadas por el parametro reutilizado de MACD 'Barras Trend Diff'")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("Umbral venta")]
		public double StochasticsA_Level_Sell {
			get { return estocasticoA_umbral_venta; }
			set { 
				if (value<80) value=80;
				if (value>100) value=100;
				estocasticoA_umbral_venta = value; 
			}
		}

		[Description("Nivel inferior del estocastico A. [0-20]. Para detectar el corte del estocastico con el umbral de compra se utilizan un numero determinado de barras anteriores, definido por el parametro 'Barras Trend Diff' de MACD")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("Umbral compra")]
		public double StochasticsA_Level_Buy {
			get { return estocasticoA_umbral_compra; }
			set { 
				if (value>20) value=20;
				if (value<0) value=0;
				estocasticoA_umbral_compra = value; 
			}
		}

		[Description("Usar la linea del estocastico A especificada.")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("Usar Linea")]
		public LineaEstocastico Linea_EstocasticoA
		{
			get { return linea_estocasticoA; }
			set { linea_estocasticoA = value; }
		}

		[Description("Definicion del valor de tiempo de la vela para el estocastico A.")]
		[GridCategory("Parametros/Luis/Estocastico A")] 
		[Gui.Design.DisplayName("Valor periodo (minutos)")]
		public int EstocasticoA_ValorPeriodo
		{
			get { return estocasticoA_valor_periodo; }
			set {  
				if (value<1) value=1;
				estocasticoA_valor_periodo = value; 
			}
		}


		
		[Description("RSI, Period")]
		[GridCategory("Parametros/Luis/RSI")] 
		[Gui.Design.DisplayName("Period")]
		public int RSI_Period {
			get { return rsi_period; }
			set { rsi_period = Math.Max(1, value); }
		}

		[Description("RSI, Smooth")]
		[GridCategory("Parametros/Luis/RSI")] 
		[Gui.Design.DisplayName("Smooth")]
		public int RSI_Smooth {
			get { return rsi_smooth; }
			set { rsi_smooth = Math.Max(1, value); }
		}
		

		[Description("RSI param para posicion corta. [50,90]")]  //// 3) Macd por encima de un parámetro (nunca menos de 0, máximo 6)
		[GridCategory("Parametros/Luis/RSI")] 
		[Gui.Design.DisplayName("Umbral venta")]
		public double RSI_Level_Sell {
			get { return RSI_umbral_venta; }
			set { 
				if (value>90) value=90;
				if (value<50) value=50;
				RSI_umbral_venta = value; 
			}
		}

		[Description("RSI param para posicion larga. [10,50]")]  //// 3) Macd por encima de un parámetro (nunca menos de 0, máximo 6)
		[GridCategory("Parametros/Luis/RSI")] 
		[Gui.Design.DisplayName("Umbral compra")]
		public double RSI_Level_Buy {
			get { return RSI_umbral_compra; }
			set { 
				if (value>50) value=50;
				if (value<10) value=10;
				RSI_umbral_compra = value; 
			}
		}

		[Description("Numero de velas donde verificar la condicion de corte del RSI con el umbral de compra/venta")]
		[GridCategory("Parametros/Luis/RSI")] 
		[Gui.Design.DisplayName("L6-num velas RSI")]
		public int L6RSIvelas {
			get { return L6_RSI_velas; }
			set { 
				if (value<1) value=1;
				L6_RSI_velas = value; 
			}
		}
	



		[Description("STORSI, Period")]
		[GridCategory("Parametros/Luis/STORSI")] 
		[Gui.Design.DisplayName("Period")]
		public int STORSI_Period {
			get { return stochasticsrsi_period; }
			set { stochasticsrsi_period = Math.Max(1, value); }
		}


		[Description("ATR, Period")]
		[GridCategory("Parametros/Luis/ATR")] 
		[Gui.Design.DisplayName("Period")]
		public int ATR_Period {
			get { return ATR_period; }
			set { ATR_period = Math.Max(1, value); }
		}

		[Description("Factor que multiplicara a la lectura ATR para calcular el valor Esp, que sera incorporado a la senial de entrada. [1.00-4.00]")]
		[GridCategory("Parametros/Luis/ATR")] 
		[Gui.Design.DisplayName("Factor amplificacion calculo Esp")]
		public double ATR_Amplificacion {
			get { return ATR_amplificacion; }
			set { 
				if (value>4) value=4;
				if (value<1) value=1;
				ATR_amplificacion = value; 
			}
		}

		[Description("ADX, Period")]
		[GridCategory("Parametros/Luis/ADX")] 
		[Gui.Design.DisplayName("Period")]
		public int ADX_Period {
			get { return adx_period; }
			set { adx_period = Math.Max(1, value); }
		}

		[Description("Las lecturas de ADX sean superiores a este limite. [20-50].")]
		[GridCategory("Parametros/Luis/ADX")] 
		[Gui.Design.DisplayName("Umbral minimo")]
		public double ADXUmbralMinimo {
			get { return ADX_umbral_minimo; }
			set { 
				if (value>50) value=50;
				if (value<20) value=20;
				ATR_amplificacion = value; 
			}
		}

		[Description("Estocastico B, D")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("D")]
		public int StochasticsB_D {
			get { return estocasticoB_D; }
			set { estocasticoB_D = Math.Max(1, value); }
		}
		[Description("Estocastico B, K")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("K")]
		public int StochasticsB_K {
			get { return estocasticoB_K; }
			set { estocasticoB_K = Math.Max(1, value); }
		}
		[Description("Estocastico B, Smooth")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("smooth")]
		public int StochasticsB_Smooth {
			get { return estocasticoB_smooth; }
			set { estocasticoB_smooth = Math.Max(1, value); }
		}

		[Description("Nivel superior del estocastico B. [70-100]. Para detectar el corte del estocastico con el umbral de venta se utilizan el numero de barras anteriores indicadas por el parametro reutilizado de MACD 'Barras Trend Diff'")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("Umbral venta")]
		public double StochasticsB_Level_Sell {
			get { return estocasticoB_umbral_venta; }
			set { 
				if (value<70) value=70;
				if (value>100) value=100;
				estocasticoB_umbral_venta = value; 
			}
		}

		[Description("Nivel inferior del estocastico. [0-30]. Para detectar el corte del estocastico con el umbral de compra se utilizan un numero determinado de barras anteriores, definido por el parametro 'Barras Trend Diff' de MACD")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("Umbral compra")]
		public double StochasticsB_Level_Buy {
			get { return estocasticoB_umbral_compra; }
			set { 
				if (value>30) value=30;
				if (value<0) value=0;

				estocasticoB_umbral_compra = value; 
			}
		}

		[Description("Usar la linea del estocastico B especificada.")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("Usar Linea")]
		public LineaEstocastico Linea_EstocasticoB
		{
			get { return linea_estocasticoB; }
			set { linea_estocasticoB = value; }
		}


		[Description("Definicion del valor de tiempo de la vela para el estocastico B1.")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("B1 Valor periodo (minutos)")]
		public int EstocasticoB1_ValorPeriodo
		{
			get { return estocasticoB1_valor_periodo; }
			set { 
				if (value<1) value=1;
				estocasticoB1_valor_periodo = value; 
			}
		}
		[Description("Definicion del valor de tiempo de la vela para el estocastico B2.")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("B2 Valor periodo (minutos)")]
		public int EstocasticoB2_ValorPeriodo
		{
			get { return estocasticoB2_valor_periodo; }
			set { 
				if (value<1) value=1;
				estocasticoB2_valor_periodo = value; 
			}
		}
		[Description("Definicion del valor de tiempo de la vela para el estocastico B3. ")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("B3 Valor periodo (minutos)")]
		public int EstocasticoB3_ValorPeriodo
		{
			get { return estocasticoB3_valor_periodo; }
			set { 
				if (value<1) value=1;
				estocasticoB3_valor_periodo = value; 
			}
		}

		[Description("Numero de estocasticos B a usar en la regla 9. [1-3]")]
		[GridCategory("Parametros/Luis/Estocastico B")] 
		[Gui.Design.DisplayName("L9: Usar numero de estocasticos")]
		public int L9numEstocasticos {
			get { return L9_num_estocasticos; }
			set { 
				if (value<1) value=1;
				if (value>3) value=3;
				L9_num_estocasticos = value; 
			}
		}





				
		[Description("Numero de barras usadas en el calculo de: tendencia(alcista/bajista). Deteccion de pico de Volumen")]
		[GridCategory("Parametros/Vicente")] 
		[Gui.Design.DisplayName("Numero de barras")]
		public int Period
		{
			get { return period; }
			set { period = Math.Max(1, value); }
		}

		[Description("Longitud mecha para deteccion de martillo. Valores porcentuales entre 0.00 y 1.00 en relacion a la longitud total de la figura.")]
		[GridCategory("Parametros/Vicente")] 
		[Gui.Design.DisplayName("Longitud de mecha")]
		public double LongMecha
		{
			get { return wick_length; }
			set { wick_length = Math.Max(0, value); }
		}

		[Description("Usar la especificada entre las distintas estrategias de trader.")]
		[GridCategory("Parametros/Comun")] 
		[Gui.Design.DisplayName("Estrategia de trading")]
		public Estrategia TradingStrategy 
		{
			get { return strategy; }
			set { strategy = value; }
		}

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
       public DataSeries EnterLong
        {
            get { return Values[1]; }
        }		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
       public DataSeries EnterShort
        {
            get { return Values[0]; }
        }
		
		
		[Description("Intervalo horario de actividad del indicador. Especificar DESDE que hora el indicador esta activo en formado HMMSS")]
		[GridCategory("Parametros/Comun")] 
		[Gui.Design.DisplayName("Intervalo de actividad. Desde.")]
		public int ActivityIntervalFrom {
			get { return activity_interval_from; }
			set { activity_interval_from = value; }
		}

		[Description("Intervalo horario de actividad del indicador. Especificar HASTA que hora el indicador esta activo en formado HMMSS")]
		[GridCategory("Parametros/Comun")] 
		[Gui.Design.DisplayName("Intervalo de actividad. Hasta.")]
		public int ActivityIntervalTo {
			get { return activity_interval_to; }
			set { activity_interval_to = value; }
		}


		[Description("Numero de contratos a comprar o vender ante senial de entrada. Un valor 0 desactivaria la gestion de contratos aunque se seguiria indicando los momentos de entrada.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Contratos entrada")]
		public int EnterContratos {
			get { return enter_contratos; }
			set { enter_contratos = value; }
		}

		[Description("Puntos de diferencia con respecto al precio de entrada que define el nivel stop loss, rebasado dicho nivel se procedera a la salida de los contratos pendientes.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Stop Loss")]
		public double StopLoss {
			get { return stop_loss; }
			set { 
				if (value<0) value=0;
//				if (value>3) value=3;
				stop_loss = value; 
			}
		}

		[Description("Puntos de diferencia con respecto al precio de entrada que define el nivel stop profit, rebasado dicho nivel se procedera a la salida de 1 contrato para reducir riesgos (Dollar Cost Averaging), tambien se movera el nivel stop_loss (trailing stop) una antidad especificada por el parametro 'Trailing Step'.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Stop Profit")]
		public double StopProfit {
			get { return stop_profit; }
			set { 
				if (value<0) value=0;
//				if (value>3) value=3;
				stop_profit = value; 
			}
		}

		[Description("Cantidad de puntos que se movea el nivel 'Stop Loss' al rebasar el nivel ´Stop Profit' (trailing stop). Valor comprendido entre [0-3]")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Trailing Step")]
		public double TrailingStep {
			get { return trailing_step; }
			set { 
				if (value<0) value=0;
				if (value>3) value=3;
				trailing_step = value; 
			}
		}

		[Description("Condicion de salida del primer contrato.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Condicion de salida contrato 1")]
		public CondicionSalida CondicionSalida_1
		{
			get { return condicion_salida_contrato1; }
			set { condicion_salida_contrato1 = value; }
		}
		[Description("Condicion de salida del segundo contrato.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Condicion de salida contrato 2")]
		public CondicionSalida CondicionSalida_2
		{
			get { return condicion_salida_contrato2; }
			set { condicion_salida_contrato2 = value; }
		}
		[Description("Condicion de salida del tercer contrato.")]
		[GridCategory("Parametros/Comun/Gestion de Contratos")] 
		[Gui.Design.DisplayName("Condicion de salida contrato 3")]
		public CondicionSalida CondicionSalida_3
		{
			get { return condicion_salida_contrato3; }
			set { condicion_salida_contrato3 = value; }
		}
		
		
    }
}

public enum Estrategia {
	Luis,
	Vicente,
}
public enum LineaEstocastico {
	D,
	K,
}
public enum CondicionSalida {
	stop_profit,
	estocastico_cruza_nivel,
	stop_profit_o_estocastico_cruza_nivel,
	estocastico_gira,
}

//----------------------------------------------------------------------------------------10 december
/*
Luis:
Para posición corta/larga:
1) Que el indicador estocástico este por encima/debajo (amarilla o mas extrema) de un nivel configurable
entre 80/5 y 95/20 (pongamos 85/15) y venga de más arriba/abajo de 85/15

2) Que el indicador macd esté por encima/debajo de un nivel configurable entre 0 y
1/-1 (pongamos 0,4/-0.4)

3) Que la diferencia entre las dos medias del macd sea menor/mayor que la
diferencia entre las dos medias del macd de un número de velas anterior
configurable entre 1 y 2 (pongamos 1)
largo, la barra sea mayor que la 1 o 2 anterior
corto, la barra sea menor que la 1 o 2 anterior


2) Poner un rango de hora seleccionable durante durante el que no de señales (por ejemplo, de 22:00 a 8:00)
   definir periodos de inactividad.


///12 dic 2014
/// 
/// determinacion de un intervalo de tiempo en que el volumen de contratos sube notablemente para despues caer,
/// si en dicho intervalo la curva de precio visto como una vela es de tipo doji entonces podriamos decir que 
/// es una zona de resistencia.
/// 
/// barra de volumen naranja - el volumen es el mayor de cualquiera de las ultimas 50 barras
/// 
///

------------------------
Plot - Indicador
1 Largo
0 No
-1 corto
------------------------

from 9:30 am to 4:15 pm EST 
Kingston (Jamaica) viernes, 12 de diciembre de 2014, 9:30:00 EST UTC-5 hours 
Madrid (Spain) viernes, 12 de diciembre de 2014, 15:30:00 CET UTC+1 hour 
Corresponding UTC (GMT) viernes, 12 de diciembre de 2014, 14:30:00     

Kingston (Jamaica) viernes, 12 de diciembre de 2014, 16:15:00 EST UTC-5 hours 
Madrid (Spain) viernes, 12 de diciembre de 2014, 22:15:00 CET UTC+1 hour 
Corresponding UTC (GMT) viernes, 12 de diciembre de 2014, 21:15:00   


3:30 pm - 10:00 pm
dia abre 8.30 am 


bajista
 doji
 martillo rojo!
 barra verde

reemplazar volumen - reusar parametro velas usadas en determinar tendencia
condicion de vol - vol sea maximo entre las N barras anteriores. 

hombro cabeza hombro --> vender

vela de intencion - sin mecha con volumen alto

volumen:

if (vol()[0]+<max(vol(),bar)[0])) return


----------------------------------------------------------------------------------------16 december


//-----------cambios

*Se crea un selector de estrategias donde se puede establecer el modo Luis o Vicente.
*Se pinta una grafica donde aparece un pulso +1/-1 por cada senial de compra/venta. 
*Se permite establecer por parametro el periodo horario de actividad.

**Modo Vicente

*parametros trend_bars y volume_bars usados para definir el humero de barras que se tienen en cuenta a la hora de determinar una tendencia alcista o bajista asi como el numero de barras que se usan al considerar el filtro de volumen respectivamente CONFLUYEN en un parametro unico llamado period.
*el filtro de volumen se cambia.
antes: queremos que el volumen sea mayor que la suma de los N anteriores
despues queremos que el volumen sea el mayor entre todos los N anteriores 

*cambio en la deteccion de giro. 
antes:	tendencia bajista/alcista(ultimas N bars) + doji O martillo up/down (sin importar rojo o verde)
despues: tend. bajista/alcista(penultimas N bars) + doji O martillo up/down rojo/verde O cualquier figura verde/roja

(calcular la tendencia incluyendo *la ultima figura* no dejaria detectar giros ya que en un giro la ultima figura rompe con la tendencia ascendente o descendente)

**Modo Luis

*Parametros de configuracion de los indicadores estandar MACD y Stochastics
*parametros para definir los umbrales de corte con el estocastico superior e inferior
*parametros para definir los umbrales de superior e inferior del MACD
*parametros para definir la tendencia del histograma MACD.Diff
*Implementacion de la logica mediante la cual se dan seniales de venta si hay tendencia adecuada en el MACD.Diff y ademas se produce un paso a traves del umbral del estocastico.

---Pendiente de implementar:
*deteccion de patron hombro cabeza hombro
*deteccion de vela de intencion





 
------------------
visita viernes 23 enero 2015

Cambios:
MMO_53859
*Regla L6. Se tiene en cuenta no el valor del RSI en la ultima vela sino en el valor MIN/MAX del RSI entre las ultimas 3 velas.
*hedge. el cruce de las marcas stop_loss y stop_profit se detecta mediente los valores high/low de la vela en vez del valor close.
*hedge. Se ha incluido en el bloque de configuracion las condiciones de salida para los contratos 1, 2 y 3
	En el MMO aparecen en el dialogo de Parametros en la seccion /parametros/comun/gestion de contratos.
*al pasar de las 22.10 se cierran todos los contratos abiertos

--
MMO_53939
*Se ha parametrizado el numero de velas que se tienen en cuenta para verificar si el RSI ha cruzado el umbral.
		El parametro L6_RSI_velas se puede encontrar en la categoria Parametros/Luis/RSI con valor por defecto a 3

*Los viejos estocastions de 1m y 5m se han renombrado como estocastico A y estocastico B respectivamente
	se hayan en la nueva categoria de parametros 
		"Parametros/Luis/Estocastico A"
		"Parametros/Luis/Estocastico B"

*Se pueden definir el valor de duracion del periodo tanto del estocastico A como del B independientemente
 mediante el parametro
		"Parametros/Luis/Estocastico A/B"
		estocasticoA_valor_periodo 1     (minutos)
		estocasticoB_valor_periodo 5

--


 

*/






#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private MMO[] cacheMMO = null;

        private static MMO checkMMO = new MMO();

        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        public MMO MMO(int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            return MMO(Input, activityIntervalFrom, activityIntervalTo, aDX_Period, aDXUmbralMinimo, aTR_Amplificacion, aTR_Period, condicionSalida_1, condicionSalida_2, condicionSalida_3, enterContratos, estocasticoA_ValorPeriodo, estocasticoB1_ValorPeriodo, estocasticoB2_ValorPeriodo, estocasticoB3_ValorPeriodo, l6RSIvelas, l9numEstocasticos, linea_EstocasticoA, linea_EstocasticoB, longMecha, mACD_Fast, mACD_Level_Buy, mACD_Level_Sell, mACD_Slow, mACD_Smooth, period, rSI_Level_Buy, rSI_Level_Sell, rSI_Period, rSI_Smooth, ruleL1, ruleL2, ruleL3, ruleL4, ruleL5, ruleL6, ruleL7, ruleL8, ruleL9, ruleV1, ruleV2, ruleV2_1, ruleV2_2, ruleV2_3, ruleV2_4, stochasticsA_D, stochasticsA_K, stochasticsA_Level_Buy, stochasticsA_Level_Sell, stochasticsA_Smooth, stochasticsB_D, stochasticsB_K, stochasticsB_Level_Buy, stochasticsB_Level_Sell, stochasticsB_Smooth, stopLoss, stopProfit, sTORSI_Period, tradingStrategy, trailingStep, trend_MACD_diff_period);
        }

        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        public MMO MMO(Data.IDataSeries input, int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            if (cacheMMO != null)
                for (int idx = 0; idx < cacheMMO.Length; idx++)
                    if (cacheMMO[idx].ActivityIntervalFrom == activityIntervalFrom && cacheMMO[idx].ActivityIntervalTo == activityIntervalTo && cacheMMO[idx].ADX_Period == aDX_Period && Math.Abs(cacheMMO[idx].ADXUmbralMinimo - aDXUmbralMinimo) <= double.Epsilon && Math.Abs(cacheMMO[idx].ATR_Amplificacion - aTR_Amplificacion) <= double.Epsilon && cacheMMO[idx].ATR_Period == aTR_Period && cacheMMO[idx].CondicionSalida_1 == condicionSalida_1 && cacheMMO[idx].CondicionSalida_2 == condicionSalida_2 && cacheMMO[idx].CondicionSalida_3 == condicionSalida_3 && cacheMMO[idx].EnterContratos == enterContratos && cacheMMO[idx].EstocasticoA_ValorPeriodo == estocasticoA_ValorPeriodo && cacheMMO[idx].EstocasticoB1_ValorPeriodo == estocasticoB1_ValorPeriodo && cacheMMO[idx].EstocasticoB2_ValorPeriodo == estocasticoB2_ValorPeriodo && cacheMMO[idx].EstocasticoB3_ValorPeriodo == estocasticoB3_ValorPeriodo && cacheMMO[idx].L6RSIvelas == l6RSIvelas && cacheMMO[idx].L9numEstocasticos == l9numEstocasticos && cacheMMO[idx].Linea_EstocasticoA == linea_EstocasticoA && cacheMMO[idx].Linea_EstocasticoB == linea_EstocasticoB && Math.Abs(cacheMMO[idx].LongMecha - longMecha) <= double.Epsilon && cacheMMO[idx].MACD_Fast == mACD_Fast && Math.Abs(cacheMMO[idx].MACD_Level_Buy - mACD_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].MACD_Level_Sell - mACD_Level_Sell) <= double.Epsilon && cacheMMO[idx].MACD_Slow == mACD_Slow && cacheMMO[idx].MACD_Smooth == mACD_Smooth && cacheMMO[idx].Period == period && Math.Abs(cacheMMO[idx].RSI_Level_Buy - rSI_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].RSI_Level_Sell - rSI_Level_Sell) <= double.Epsilon && cacheMMO[idx].RSI_Period == rSI_Period && cacheMMO[idx].RSI_Smooth == rSI_Smooth && cacheMMO[idx].RuleL1 == ruleL1 && cacheMMO[idx].RuleL2 == ruleL2 && cacheMMO[idx].RuleL3 == ruleL3 && cacheMMO[idx].RuleL4 == ruleL4 && cacheMMO[idx].RuleL5 == ruleL5 && cacheMMO[idx].RuleL6 == ruleL6 && cacheMMO[idx].RuleL7 == ruleL7 && cacheMMO[idx].RuleL8 == ruleL8 && cacheMMO[idx].RuleL9 == ruleL9 && cacheMMO[idx].RuleV1 == ruleV1 && cacheMMO[idx].RuleV2 == ruleV2 && cacheMMO[idx].RuleV2_1 == ruleV2_1 && cacheMMO[idx].RuleV2_2 == ruleV2_2 && cacheMMO[idx].RuleV2_3 == ruleV2_3 && cacheMMO[idx].RuleV2_4 == ruleV2_4 && cacheMMO[idx].StochasticsA_D == stochasticsA_D && cacheMMO[idx].StochasticsA_K == stochasticsA_K && Math.Abs(cacheMMO[idx].StochasticsA_Level_Buy - stochasticsA_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].StochasticsA_Level_Sell - stochasticsA_Level_Sell) <= double.Epsilon && cacheMMO[idx].StochasticsA_Smooth == stochasticsA_Smooth && cacheMMO[idx].StochasticsB_D == stochasticsB_D && cacheMMO[idx].StochasticsB_K == stochasticsB_K && Math.Abs(cacheMMO[idx].StochasticsB_Level_Buy - stochasticsB_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].StochasticsB_Level_Sell - stochasticsB_Level_Sell) <= double.Epsilon && cacheMMO[idx].StochasticsB_Smooth == stochasticsB_Smooth && Math.Abs(cacheMMO[idx].StopLoss - stopLoss) <= double.Epsilon && Math.Abs(cacheMMO[idx].StopProfit - stopProfit) <= double.Epsilon && cacheMMO[idx].STORSI_Period == sTORSI_Period && cacheMMO[idx].TradingStrategy == tradingStrategy && Math.Abs(cacheMMO[idx].TrailingStep - trailingStep) <= double.Epsilon && cacheMMO[idx].Trend_MACD_diff_period == trend_MACD_diff_period && cacheMMO[idx].EqualsInput(input))
                        return cacheMMO[idx];

            lock (checkMMO)
            {
                checkMMO.ActivityIntervalFrom = activityIntervalFrom;
                activityIntervalFrom = checkMMO.ActivityIntervalFrom;
                checkMMO.ActivityIntervalTo = activityIntervalTo;
                activityIntervalTo = checkMMO.ActivityIntervalTo;
                checkMMO.ADX_Period = aDX_Period;
                aDX_Period = checkMMO.ADX_Period;
                checkMMO.ADXUmbralMinimo = aDXUmbralMinimo;
                aDXUmbralMinimo = checkMMO.ADXUmbralMinimo;
                checkMMO.ATR_Amplificacion = aTR_Amplificacion;
                aTR_Amplificacion = checkMMO.ATR_Amplificacion;
                checkMMO.ATR_Period = aTR_Period;
                aTR_Period = checkMMO.ATR_Period;
                checkMMO.CondicionSalida_1 = condicionSalida_1;
                condicionSalida_1 = checkMMO.CondicionSalida_1;
                checkMMO.CondicionSalida_2 = condicionSalida_2;
                condicionSalida_2 = checkMMO.CondicionSalida_2;
                checkMMO.CondicionSalida_3 = condicionSalida_3;
                condicionSalida_3 = checkMMO.CondicionSalida_3;
                checkMMO.EnterContratos = enterContratos;
                enterContratos = checkMMO.EnterContratos;
                checkMMO.EstocasticoA_ValorPeriodo = estocasticoA_ValorPeriodo;
                estocasticoA_ValorPeriodo = checkMMO.EstocasticoA_ValorPeriodo;
                checkMMO.EstocasticoB1_ValorPeriodo = estocasticoB1_ValorPeriodo;
                estocasticoB1_ValorPeriodo = checkMMO.EstocasticoB1_ValorPeriodo;
                checkMMO.EstocasticoB2_ValorPeriodo = estocasticoB2_ValorPeriodo;
                estocasticoB2_ValorPeriodo = checkMMO.EstocasticoB2_ValorPeriodo;
                checkMMO.EstocasticoB3_ValorPeriodo = estocasticoB3_ValorPeriodo;
                estocasticoB3_ValorPeriodo = checkMMO.EstocasticoB3_ValorPeriodo;
                checkMMO.L6RSIvelas = l6RSIvelas;
                l6RSIvelas = checkMMO.L6RSIvelas;
                checkMMO.L9numEstocasticos = l9numEstocasticos;
                l9numEstocasticos = checkMMO.L9numEstocasticos;
                checkMMO.Linea_EstocasticoA = linea_EstocasticoA;
                linea_EstocasticoA = checkMMO.Linea_EstocasticoA;
                checkMMO.Linea_EstocasticoB = linea_EstocasticoB;
                linea_EstocasticoB = checkMMO.Linea_EstocasticoB;
                checkMMO.LongMecha = longMecha;
                longMecha = checkMMO.LongMecha;
                checkMMO.MACD_Fast = mACD_Fast;
                mACD_Fast = checkMMO.MACD_Fast;
                checkMMO.MACD_Level_Buy = mACD_Level_Buy;
                mACD_Level_Buy = checkMMO.MACD_Level_Buy;
                checkMMO.MACD_Level_Sell = mACD_Level_Sell;
                mACD_Level_Sell = checkMMO.MACD_Level_Sell;
                checkMMO.MACD_Slow = mACD_Slow;
                mACD_Slow = checkMMO.MACD_Slow;
                checkMMO.MACD_Smooth = mACD_Smooth;
                mACD_Smooth = checkMMO.MACD_Smooth;
                checkMMO.Period = period;
                period = checkMMO.Period;
                checkMMO.RSI_Level_Buy = rSI_Level_Buy;
                rSI_Level_Buy = checkMMO.RSI_Level_Buy;
                checkMMO.RSI_Level_Sell = rSI_Level_Sell;
                rSI_Level_Sell = checkMMO.RSI_Level_Sell;
                checkMMO.RSI_Period = rSI_Period;
                rSI_Period = checkMMO.RSI_Period;
                checkMMO.RSI_Smooth = rSI_Smooth;
                rSI_Smooth = checkMMO.RSI_Smooth;
                checkMMO.RuleL1 = ruleL1;
                ruleL1 = checkMMO.RuleL1;
                checkMMO.RuleL2 = ruleL2;
                ruleL2 = checkMMO.RuleL2;
                checkMMO.RuleL3 = ruleL3;
                ruleL3 = checkMMO.RuleL3;
                checkMMO.RuleL4 = ruleL4;
                ruleL4 = checkMMO.RuleL4;
                checkMMO.RuleL5 = ruleL5;
                ruleL5 = checkMMO.RuleL5;
                checkMMO.RuleL6 = ruleL6;
                ruleL6 = checkMMO.RuleL6;
                checkMMO.RuleL7 = ruleL7;
                ruleL7 = checkMMO.RuleL7;
                checkMMO.RuleL8 = ruleL8;
                ruleL8 = checkMMO.RuleL8;
                checkMMO.RuleL9 = ruleL9;
                ruleL9 = checkMMO.RuleL9;
                checkMMO.RuleV1 = ruleV1;
                ruleV1 = checkMMO.RuleV1;
                checkMMO.RuleV2 = ruleV2;
                ruleV2 = checkMMO.RuleV2;
                checkMMO.RuleV2_1 = ruleV2_1;
                ruleV2_1 = checkMMO.RuleV2_1;
                checkMMO.RuleV2_2 = ruleV2_2;
                ruleV2_2 = checkMMO.RuleV2_2;
                checkMMO.RuleV2_3 = ruleV2_3;
                ruleV2_3 = checkMMO.RuleV2_3;
                checkMMO.RuleV2_4 = ruleV2_4;
                ruleV2_4 = checkMMO.RuleV2_4;
                checkMMO.StochasticsA_D = stochasticsA_D;
                stochasticsA_D = checkMMO.StochasticsA_D;
                checkMMO.StochasticsA_K = stochasticsA_K;
                stochasticsA_K = checkMMO.StochasticsA_K;
                checkMMO.StochasticsA_Level_Buy = stochasticsA_Level_Buy;
                stochasticsA_Level_Buy = checkMMO.StochasticsA_Level_Buy;
                checkMMO.StochasticsA_Level_Sell = stochasticsA_Level_Sell;
                stochasticsA_Level_Sell = checkMMO.StochasticsA_Level_Sell;
                checkMMO.StochasticsA_Smooth = stochasticsA_Smooth;
                stochasticsA_Smooth = checkMMO.StochasticsA_Smooth;
                checkMMO.StochasticsB_D = stochasticsB_D;
                stochasticsB_D = checkMMO.StochasticsB_D;
                checkMMO.StochasticsB_K = stochasticsB_K;
                stochasticsB_K = checkMMO.StochasticsB_K;
                checkMMO.StochasticsB_Level_Buy = stochasticsB_Level_Buy;
                stochasticsB_Level_Buy = checkMMO.StochasticsB_Level_Buy;
                checkMMO.StochasticsB_Level_Sell = stochasticsB_Level_Sell;
                stochasticsB_Level_Sell = checkMMO.StochasticsB_Level_Sell;
                checkMMO.StochasticsB_Smooth = stochasticsB_Smooth;
                stochasticsB_Smooth = checkMMO.StochasticsB_Smooth;
                checkMMO.StopLoss = stopLoss;
                stopLoss = checkMMO.StopLoss;
                checkMMO.StopProfit = stopProfit;
                stopProfit = checkMMO.StopProfit;
                checkMMO.STORSI_Period = sTORSI_Period;
                sTORSI_Period = checkMMO.STORSI_Period;
                checkMMO.TradingStrategy = tradingStrategy;
                tradingStrategy = checkMMO.TradingStrategy;
                checkMMO.TrailingStep = trailingStep;
                trailingStep = checkMMO.TrailingStep;
                checkMMO.Trend_MACD_diff_period = trend_MACD_diff_period;
                trend_MACD_diff_period = checkMMO.Trend_MACD_diff_period;

                if (cacheMMO != null)
                    for (int idx = 0; idx < cacheMMO.Length; idx++)
                        if (cacheMMO[idx].ActivityIntervalFrom == activityIntervalFrom && cacheMMO[idx].ActivityIntervalTo == activityIntervalTo && cacheMMO[idx].ADX_Period == aDX_Period && Math.Abs(cacheMMO[idx].ADXUmbralMinimo - aDXUmbralMinimo) <= double.Epsilon && Math.Abs(cacheMMO[idx].ATR_Amplificacion - aTR_Amplificacion) <= double.Epsilon && cacheMMO[idx].ATR_Period == aTR_Period && cacheMMO[idx].CondicionSalida_1 == condicionSalida_1 && cacheMMO[idx].CondicionSalida_2 == condicionSalida_2 && cacheMMO[idx].CondicionSalida_3 == condicionSalida_3 && cacheMMO[idx].EnterContratos == enterContratos && cacheMMO[idx].EstocasticoA_ValorPeriodo == estocasticoA_ValorPeriodo && cacheMMO[idx].EstocasticoB1_ValorPeriodo == estocasticoB1_ValorPeriodo && cacheMMO[idx].EstocasticoB2_ValorPeriodo == estocasticoB2_ValorPeriodo && cacheMMO[idx].EstocasticoB3_ValorPeriodo == estocasticoB3_ValorPeriodo && cacheMMO[idx].L6RSIvelas == l6RSIvelas && cacheMMO[idx].L9numEstocasticos == l9numEstocasticos && cacheMMO[idx].Linea_EstocasticoA == linea_EstocasticoA && cacheMMO[idx].Linea_EstocasticoB == linea_EstocasticoB && Math.Abs(cacheMMO[idx].LongMecha - longMecha) <= double.Epsilon && cacheMMO[idx].MACD_Fast == mACD_Fast && Math.Abs(cacheMMO[idx].MACD_Level_Buy - mACD_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].MACD_Level_Sell - mACD_Level_Sell) <= double.Epsilon && cacheMMO[idx].MACD_Slow == mACD_Slow && cacheMMO[idx].MACD_Smooth == mACD_Smooth && cacheMMO[idx].Period == period && Math.Abs(cacheMMO[idx].RSI_Level_Buy - rSI_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].RSI_Level_Sell - rSI_Level_Sell) <= double.Epsilon && cacheMMO[idx].RSI_Period == rSI_Period && cacheMMO[idx].RSI_Smooth == rSI_Smooth && cacheMMO[idx].RuleL1 == ruleL1 && cacheMMO[idx].RuleL2 == ruleL2 && cacheMMO[idx].RuleL3 == ruleL3 && cacheMMO[idx].RuleL4 == ruleL4 && cacheMMO[idx].RuleL5 == ruleL5 && cacheMMO[idx].RuleL6 == ruleL6 && cacheMMO[idx].RuleL7 == ruleL7 && cacheMMO[idx].RuleL8 == ruleL8 && cacheMMO[idx].RuleL9 == ruleL9 && cacheMMO[idx].RuleV1 == ruleV1 && cacheMMO[idx].RuleV2 == ruleV2 && cacheMMO[idx].RuleV2_1 == ruleV2_1 && cacheMMO[idx].RuleV2_2 == ruleV2_2 && cacheMMO[idx].RuleV2_3 == ruleV2_3 && cacheMMO[idx].RuleV2_4 == ruleV2_4 && cacheMMO[idx].StochasticsA_D == stochasticsA_D && cacheMMO[idx].StochasticsA_K == stochasticsA_K && Math.Abs(cacheMMO[idx].StochasticsA_Level_Buy - stochasticsA_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].StochasticsA_Level_Sell - stochasticsA_Level_Sell) <= double.Epsilon && cacheMMO[idx].StochasticsA_Smooth == stochasticsA_Smooth && cacheMMO[idx].StochasticsB_D == stochasticsB_D && cacheMMO[idx].StochasticsB_K == stochasticsB_K && Math.Abs(cacheMMO[idx].StochasticsB_Level_Buy - stochasticsB_Level_Buy) <= double.Epsilon && Math.Abs(cacheMMO[idx].StochasticsB_Level_Sell - stochasticsB_Level_Sell) <= double.Epsilon && cacheMMO[idx].StochasticsB_Smooth == stochasticsB_Smooth && Math.Abs(cacheMMO[idx].StopLoss - stopLoss) <= double.Epsilon && Math.Abs(cacheMMO[idx].StopProfit - stopProfit) <= double.Epsilon && cacheMMO[idx].STORSI_Period == sTORSI_Period && cacheMMO[idx].TradingStrategy == tradingStrategy && Math.Abs(cacheMMO[idx].TrailingStep - trailingStep) <= double.Epsilon && cacheMMO[idx].Trend_MACD_diff_period == trend_MACD_diff_period && cacheMMO[idx].EqualsInput(input))
                            return cacheMMO[idx];

                MMO indicator = new MMO();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.ActivityIntervalFrom = activityIntervalFrom;
                indicator.ActivityIntervalTo = activityIntervalTo;
                indicator.ADX_Period = aDX_Period;
                indicator.ADXUmbralMinimo = aDXUmbralMinimo;
                indicator.ATR_Amplificacion = aTR_Amplificacion;
                indicator.ATR_Period = aTR_Period;
                indicator.CondicionSalida_1 = condicionSalida_1;
                indicator.CondicionSalida_2 = condicionSalida_2;
                indicator.CondicionSalida_3 = condicionSalida_3;
                indicator.EnterContratos = enterContratos;
                indicator.EstocasticoA_ValorPeriodo = estocasticoA_ValorPeriodo;
                indicator.EstocasticoB1_ValorPeriodo = estocasticoB1_ValorPeriodo;
                indicator.EstocasticoB2_ValorPeriodo = estocasticoB2_ValorPeriodo;
                indicator.EstocasticoB3_ValorPeriodo = estocasticoB3_ValorPeriodo;
                indicator.L6RSIvelas = l6RSIvelas;
                indicator.L9numEstocasticos = l9numEstocasticos;
                indicator.Linea_EstocasticoA = linea_EstocasticoA;
                indicator.Linea_EstocasticoB = linea_EstocasticoB;
                indicator.LongMecha = longMecha;
                indicator.MACD_Fast = mACD_Fast;
                indicator.MACD_Level_Buy = mACD_Level_Buy;
                indicator.MACD_Level_Sell = mACD_Level_Sell;
                indicator.MACD_Slow = mACD_Slow;
                indicator.MACD_Smooth = mACD_Smooth;
                indicator.Period = period;
                indicator.RSI_Level_Buy = rSI_Level_Buy;
                indicator.RSI_Level_Sell = rSI_Level_Sell;
                indicator.RSI_Period = rSI_Period;
                indicator.RSI_Smooth = rSI_Smooth;
                indicator.RuleL1 = ruleL1;
                indicator.RuleL2 = ruleL2;
                indicator.RuleL3 = ruleL3;
                indicator.RuleL4 = ruleL4;
                indicator.RuleL5 = ruleL5;
                indicator.RuleL6 = ruleL6;
                indicator.RuleL7 = ruleL7;
                indicator.RuleL8 = ruleL8;
                indicator.RuleL9 = ruleL9;
                indicator.RuleV1 = ruleV1;
                indicator.RuleV2 = ruleV2;
                indicator.RuleV2_1 = ruleV2_1;
                indicator.RuleV2_2 = ruleV2_2;
                indicator.RuleV2_3 = ruleV2_3;
                indicator.RuleV2_4 = ruleV2_4;
                indicator.StochasticsA_D = stochasticsA_D;
                indicator.StochasticsA_K = stochasticsA_K;
                indicator.StochasticsA_Level_Buy = stochasticsA_Level_Buy;
                indicator.StochasticsA_Level_Sell = stochasticsA_Level_Sell;
                indicator.StochasticsA_Smooth = stochasticsA_Smooth;
                indicator.StochasticsB_D = stochasticsB_D;
                indicator.StochasticsB_K = stochasticsB_K;
                indicator.StochasticsB_Level_Buy = stochasticsB_Level_Buy;
                indicator.StochasticsB_Level_Sell = stochasticsB_Level_Sell;
                indicator.StochasticsB_Smooth = stochasticsB_Smooth;
                indicator.StopLoss = stopLoss;
                indicator.StopProfit = stopProfit;
                indicator.STORSI_Period = sTORSI_Period;
                indicator.TradingStrategy = tradingStrategy;
                indicator.TrailingStep = trailingStep;
                indicator.Trend_MACD_diff_period = trend_MACD_diff_period;
                Indicators.Add(indicator);
                indicator.SetUp();

                MMO[] tmp = new MMO[cacheMMO == null ? 1 : cacheMMO.Length + 1];
                if (cacheMMO != null)
                    cacheMMO.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheMMO = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.MMO MMO(int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            return _indicator.MMO(Input, activityIntervalFrom, activityIntervalTo, aDX_Period, aDXUmbralMinimo, aTR_Amplificacion, aTR_Period, condicionSalida_1, condicionSalida_2, condicionSalida_3, enterContratos, estocasticoA_ValorPeriodo, estocasticoB1_ValorPeriodo, estocasticoB2_ValorPeriodo, estocasticoB3_ValorPeriodo, l6RSIvelas, l9numEstocasticos, linea_EstocasticoA, linea_EstocasticoB, longMecha, mACD_Fast, mACD_Level_Buy, mACD_Level_Sell, mACD_Slow, mACD_Smooth, period, rSI_Level_Buy, rSI_Level_Sell, rSI_Period, rSI_Smooth, ruleL1, ruleL2, ruleL3, ruleL4, ruleL5, ruleL6, ruleL7, ruleL8, ruleL9, ruleV1, ruleV2, ruleV2_1, ruleV2_2, ruleV2_3, ruleV2_4, stochasticsA_D, stochasticsA_K, stochasticsA_Level_Buy, stochasticsA_Level_Sell, stochasticsA_Smooth, stochasticsB_D, stochasticsB_K, stochasticsB_Level_Buy, stochasticsB_Level_Sell, stochasticsB_Smooth, stopLoss, stopProfit, sTORSI_Period, tradingStrategy, trailingStep, trend_MACD_diff_period);
        }

        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        public Indicator.MMO MMO(Data.IDataSeries input, int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            return _indicator.MMO(input, activityIntervalFrom, activityIntervalTo, aDX_Period, aDXUmbralMinimo, aTR_Amplificacion, aTR_Period, condicionSalida_1, condicionSalida_2, condicionSalida_3, enterContratos, estocasticoA_ValorPeriodo, estocasticoB1_ValorPeriodo, estocasticoB2_ValorPeriodo, estocasticoB3_ValorPeriodo, l6RSIvelas, l9numEstocasticos, linea_EstocasticoA, linea_EstocasticoB, longMecha, mACD_Fast, mACD_Level_Buy, mACD_Level_Sell, mACD_Slow, mACD_Smooth, period, rSI_Level_Buy, rSI_Level_Sell, rSI_Period, rSI_Smooth, ruleL1, ruleL2, ruleL3, ruleL4, ruleL5, ruleL6, ruleL7, ruleL8, ruleL9, ruleV1, ruleV2, ruleV2_1, ruleV2_2, ruleV2_3, ruleV2_4, stochasticsA_D, stochasticsA_K, stochasticsA_Level_Buy, stochasticsA_Level_Sell, stochasticsA_Smooth, stochasticsB_D, stochasticsB_K, stochasticsB_Level_Buy, stochasticsB_Level_Sell, stochasticsB_Smooth, stopLoss, stopProfit, sTORSI_Period, tradingStrategy, trailingStep, trend_MACD_diff_period);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.MMO MMO(int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            return _indicator.MMO(Input, activityIntervalFrom, activityIntervalTo, aDX_Period, aDXUmbralMinimo, aTR_Amplificacion, aTR_Period, condicionSalida_1, condicionSalida_2, condicionSalida_3, enterContratos, estocasticoA_ValorPeriodo, estocasticoB1_ValorPeriodo, estocasticoB2_ValorPeriodo, estocasticoB3_ValorPeriodo, l6RSIvelas, l9numEstocasticos, linea_EstocasticoA, linea_EstocasticoB, longMecha, mACD_Fast, mACD_Level_Buy, mACD_Level_Sell, mACD_Slow, mACD_Smooth, period, rSI_Level_Buy, rSI_Level_Sell, rSI_Period, rSI_Smooth, ruleL1, ruleL2, ruleL3, ruleL4, ruleL5, ruleL6, ruleL7, ruleL8, ruleL9, ruleV1, ruleV2, ruleV2_1, ruleV2_2, ruleV2_3, ruleV2_4, stochasticsA_D, stochasticsA_K, stochasticsA_Level_Buy, stochasticsA_Level_Sell, stochasticsA_Smooth, stochasticsB_D, stochasticsB_K, stochasticsB_Level_Buy, stochasticsB_Level_Sell, stochasticsB_Smooth, stopLoss, stopProfit, sTORSI_Period, tradingStrategy, trailingStep, trend_MACD_diff_period);
        }

        /// <summary>
        /// Martin Miller Oscilator
        /// </summary>
        /// <returns></returns>
        public Indicator.MMO MMO(Data.IDataSeries input, int activityIntervalFrom, int activityIntervalTo, int aDX_Period, double aDXUmbralMinimo, double aTR_Amplificacion, int aTR_Period, CondicionSalida condicionSalida_1, CondicionSalida condicionSalida_2, CondicionSalida condicionSalida_3, int enterContratos, int estocasticoA_ValorPeriodo, int estocasticoB1_ValorPeriodo, int estocasticoB2_ValorPeriodo, int estocasticoB3_ValorPeriodo, int l6RSIvelas, int l9numEstocasticos, LineaEstocastico linea_EstocasticoA, LineaEstocastico linea_EstocasticoB, double longMecha, int mACD_Fast, double mACD_Level_Buy, double mACD_Level_Sell, int mACD_Slow, int mACD_Smooth, int period, double rSI_Level_Buy, double rSI_Level_Sell, int rSI_Period, int rSI_Smooth, bool ruleL1, bool ruleL2, bool ruleL3, bool ruleL4, bool ruleL5, bool ruleL6, bool ruleL7, bool ruleL8, bool ruleL9, bool ruleV1, bool ruleV2, bool ruleV2_1, bool ruleV2_2, bool ruleV2_3, bool ruleV2_4, int stochasticsA_D, int stochasticsA_K, double stochasticsA_Level_Buy, double stochasticsA_Level_Sell, int stochasticsA_Smooth, int stochasticsB_D, int stochasticsB_K, double stochasticsB_Level_Buy, double stochasticsB_Level_Sell, int stochasticsB_Smooth, double stopLoss, double stopProfit, int sTORSI_Period, Estrategia tradingStrategy, double trailingStep, int trend_MACD_diff_period)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.MMO(input, activityIntervalFrom, activityIntervalTo, aDX_Period, aDXUmbralMinimo, aTR_Amplificacion, aTR_Period, condicionSalida_1, condicionSalida_2, condicionSalida_3, enterContratos, estocasticoA_ValorPeriodo, estocasticoB1_ValorPeriodo, estocasticoB2_ValorPeriodo, estocasticoB3_ValorPeriodo, l6RSIvelas, l9numEstocasticos, linea_EstocasticoA, linea_EstocasticoB, longMecha, mACD_Fast, mACD_Level_Buy, mACD_Level_Sell, mACD_Slow, mACD_Smooth, period, rSI_Level_Buy, rSI_Level_Sell, rSI_Period, rSI_Smooth, ruleL1, ruleL2, ruleL3, ruleL4, ruleL5, ruleL6, ruleL7, ruleL8, ruleL9, ruleV1, ruleV2, ruleV2_1, ruleV2_2, ruleV2_3, ruleV2_4, stochasticsA_D, stochasticsA_K, stochasticsA_Level_Buy, stochasticsA_Level_Sell, stochasticsA_Smooth, stochasticsB_D, stochasticsB_K, stochasticsB_Level_Buy, stochasticsB_Level_Sell, stochasticsB_Smooth, stopLoss, stopProfit, sTORSI_Period, tradingStrategy, trailingStep, trend_MACD_diff_period);
        }
    }
}
#endregion
