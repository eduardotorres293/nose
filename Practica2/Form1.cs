using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Practica2
{
    public partial class Form1 : Form
    {
        private readonly List<string> P_Reservadas = new List<string>
        {
            "auto", "break", "case", "char", "const", "continue", "default", "do",
            "double", "else", "enum", "extern", "float", "for", "goto", "if", "include",
            "inline", "int", "long", "main", "register", "restrict", "return", "short",
            "signed", "sizeof", "static", "struct", "switch", "typedef", "union",
            "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof",
            "_Atomic", "_Bool", "_Complex", "_Generic", "_Imaginary", "_Noreturn",
            "_Static_assert", "_Thread_local"
        };

        private List<SimboloFuncion> TablaFunciones = new List<SimboloFuncion>();
        private Stack<List<SimboloVariable>> PilaAmbitos = new Stack<List<SimboloVariable>>();

        class NodoExpresion
        {
            public string Valor;
            public NodoExpresion Izquierdo;
            public NodoExpresion Derecho;
            public double? ValorNumerico;
            public string TipoInferido;

            public NodoExpresion(string valor, NodoExpresion izq = null, NodoExpresion der = null)
            {
                Valor = valor;
                Izquierdo = izq;
                Derecho = der;
                ValorNumerico = null;
                TipoInferido = "?";
            }
        }

        class SimboloVariable
        {
            public string Nombre;
            public string Tipo;
            public int Direccion;

            public SimboloVariable(string nombre, string tipo, int direccion)
            {
                Nombre = nombre;
                Tipo = tipo;
                Direccion = direccion;
            }
        }

        class SimboloFuncion
        {
            public string Nombre;
            public string TipoRetorno;
            public int NumeroParametros;
            public List<string> TiposParametros;

            public SimboloFuncion(string nombre, string tipoRetorno)
            {
                Nombre = nombre;
                TipoRetorno = tipoRetorno;
                NumeroParametros = 0;
                TiposParametros = new List<string>();
            }
        }

        public Form1()
        {
            InitializeComponent();
            EntrarAmbito();
            TablaFunciones.Add(new SimboloFuncion("printf", "int"));
            analizarToolStripMenuItem.Enabled = false;
        }

        private double? EvaluarNodo(NodoExpresion nodo)
        {
            if (nodo == null) return null;

            if (nodo.Valor == "numero" || nodo.Valor == "numero_real")
                return nodo.ValorNumerico;

            if (nodo.Valor == "true") return 1;
            if (nodo.Valor == "false") return 0;
            if (nodo.Valor == "()")
                return EvaluarNodo(nodo.Izquierdo);

            double? izq = EvaluarNodo(nodo.Izquierdo);
            double? der = EvaluarNodo(nodo.Derecho);

            if (izq == null || der == null) return null;

            switch (nodo.Valor)
            {
                case "+": return izq + der;
                case "-": return izq - der;
                case "*": return izq * der;
                case "/": return der != 0 ? izq / der : (double?)null;
                case "%": return der != 0 ? izq % der : (double?)null;
                case "==": return izq == der ? 1 : 0;
                case "!=": return izq != der ? 1 : 0;
                case "<": return izq < der ? 1 : 0;
                case ">": return izq > der ? 1 : 0;
                case "<=": return izq <= der ? 1 : 0;
                case ">=": return izq >= der ? 1 : 0;
                case "&&": return (izq != 0 && der != 0) ? 1 : 0;
                case "||": return (izq != 0 || der != 0) ? 1 : 0;
                default: return null;
            }
        }

        private string InferirTipo(NodoExpresion nodo)
        {
            if (nodo == null) return "?";

            switch (nodo.Valor)
            {
                case "numero": return "int";
                case "numero_real": return "float";
                case "caracter": return "char";
                case "Cadena": return "string";
                case "true":
                case "false": return "bool";
            }
            if (nodo.Valor == "()")
                return InferirTipo(nodo.Izquierdo);

            if (nodo.Valor == "==" || nodo.Valor == "!=" ||
                nodo.Valor == "<" || nodo.Valor == ">" ||
                nodo.Valor == "<=" || nodo.Valor == ">=" ||
                nodo.Valor == "&&" || nodo.Valor == "||")
                return "bool";

            if (nodo.Valor == "+" || nodo.Valor == "-" ||
                nodo.Valor == "*" || nodo.Valor == "/" || nodo.Valor == "%")
            {
                string tIzq = InferirTipo(nodo.Izquierdo);
                string tDer = InferirTipo(nodo.Derecho);

                if (tIzq == "string" || tDer == "string") return "string";
                if (tIzq == "float" || tDer == "float") return "float";
                if (tIzq == "int" && tDer == "int") return "int";
                return "?";
            }

            if (nodo.Valor.StartsWith("func:")) return "?";

            foreach (var ambito in PilaAmbitos)
            {
                var sim = ambito.FirstOrDefault(v => v.Nombre == nodo.Valor);
                if (sim != null) return sim.Tipo;
            }

            return "?";
        }

        private void VerificarTipos(NodoExpresion nodo)
        {
            if (nodo == null) return;

            VerificarTipos(nodo.Izquierdo);
            VerificarTipos(nodo.Derecho);

            if (nodo.Izquierdo == null || nodo.Derecho == null) return;

            string tIzq = InferirTipo(nodo.Izquierdo);
            string tDer = InferirTipo(nodo.Derecho);

            if ((nodo.Valor == "-" || nodo.Valor == "*" || nodo.Valor == "/" || nodo.Valor == "%") &&
                (tIzq == "string" || tDer == "string"))
            {
                richTextBox2.AppendText(
                    $"Advertencia tipos: operador '{nodo.Valor}' no aplica a string, línea {Numero_linea}\n");
            }

            if ((nodo.Valor == "+" || nodo.Valor == "-" || nodo.Valor == "*" || nodo.Valor == "/") &&
                (tIzq == "bool" || tDer == "bool"))
            {
                richTextBox2.AppendText(
                    $"Advertencia tipos: operador '{nodo.Valor}' mezcla booleano con numérico, línea {Numero_linea}\n");
            }
        }

        private void EntrarAmbito() => PilaAmbitos.Push(new List<SimboloVariable>());

        private void SalirAmbito()
        {
            if (PilaAmbitos.Count > 1)
                PilaAmbitos.Pop();
        }

        private bool ExisteVariableEnAmbitoActual(string nombre)
        {
            return PilaAmbitos.Count > 0 && PilaAmbitos.Peek().Any(v => v.Nombre == nombre);
        }

        private bool VariableDeclarada(string nombre)
        {
            return PilaAmbitos.Any(ambito => ambito.Any(v => v.Nombre == nombre));
        }

        private bool ExisteFuncion(string nombre)
        {
            return TablaFunciones.Any(f => f.Nombre == nombre);
        }

        private void AgregarVariable(string nombre)
        {
            PilaAmbitos.Peek().Add(new SimboloVariable(nombre, tipoActual, direccionActual));
            direccionActual += 4;
        }

        private void ImprimirArbol(NodoExpresion nodo, string prefijo = "", bool esRaiz = true)
        {
            if (nodo == null) return;

            if (esRaiz)
            {
                string tipoRaiz = InferirTipo(nodo);
                double? resultado = EvaluarNodo(nodo);
                VerificarTipos(nodo);

                richTextBox2.AppendText($"  ┌─ Árbol de expresión ─────────────────\n");
                richTextBox2.AppendText($"  │  Nodo raíz  : {nodo.Valor}\n");
                richTextBox2.AppendText($"  │  Tipo       : {tipoRaiz}\n");

                if (resultado.HasValue)
                {
                    string resStr = (resultado.Value == Math.Floor(resultado.Value))
                        ? ((long)resultado.Value).ToString()
                        : resultado.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                    richTextBox2.AppendText($"  │  Evaluación : {resStr}\n");
                }
                else
                {
                    richTextBox2.AppendText($"  │  Evaluación : (no evaluable en tiempo de análisis)\n");
                }

                richTextBox2.AppendText($"  └───────────────────────────────────\n");
                //richTextBox2.AppendText($"  Árbol: {nodo.Valor}\n");
            }
            //else
            //{
            //    richTextBox2.AppendText($"{prefijo}└── {nodo.Valor}\n");
            //}

            string nuevoPrefijo = esRaiz ? "      " : prefijo + "    ";
            //ImprimirArbol(nodo.Izquierdo, nuevoPrefijo, false);
            //ImprimirArbol(nodo.Derecho, nuevoPrefijo, false);
        }
        private bool EsInicioOperando()
        {
            return token == "numero" ||
                   token == "numero_real" ||
                   token == "caracter" ||
                   token == "Cadena" ||
                   token == "identificador" ||
                   token == "true" ||
                   token == "false" ||
                   token == "++" ||
                   token == "--" ||
                   token == "(";
        }

        private bool EsOperador()
        {
            return token == "+" || token == "-" ||
                   token == "*" || token == "/" || token == "%" ||
                   token == "++" || token == "--" ||
                   token == "==" || token == "!=" ||
                   token == "<" || token == ">" ||
                   token == "<=" || token == ">=" ||
                   token == "&&" || token == "||";
        }

        private NodoExpresion Operando()
        {
            if (token == "++" || token == "--")
            {
                string opPrefijo = token;
                Avanzar();

                if (token != "identificador")
                {
                    Error($"Se esperaba un identificador después de '{opPrefijo}'");
                    return new NodoExpresion("?");
                }

                string nombrePre = valorToken;
                if (!VariableDeclarada(nombrePre))
                    Error($"La variable '{nombrePre}' no ha sido declarada");

                Avanzar();
                return new NodoExpresion(opPrefijo + nombrePre);
            }
            if (token == "numero")
            {
                NodoExpresion n = new NodoExpresion("numero");
                n.ValorNumerico = valorNumericoToken;
                n.TipoInferido = "int";
                Avanzar();
                return n;
            }
            if (token == "numero_real")
            {
                NodoExpresion n = new NodoExpresion("numero_real");
                n.ValorNumerico = valorNumericoToken;
                n.TipoInferido = "float";
                Avanzar();
                return n;
            }
            if (token == "caracter")
            {
                NodoExpresion n = new NodoExpresion("caracter");
                Avanzar();
                return n;
            }
            if (token == "Cadena")
            {
                NodoExpresion n = new NodoExpresion("Cadena");
                Avanzar();
                return n;
            }
            if (token == "true" || token == "false")
            {
                NodoExpresion n = new NodoExpresion(token);
                Avanzar();
                return n;
            }
            if (token == "identificador")
            {
                string nombre = valorToken;
                Avanzar();

                if (token == "(")
                {
                    if (!ExisteFuncion(nombre))
                        Error($"La función '{nombre}' no ha sido declarada");

                    NodoExpresion nodoLlamada = new NodoExpresion($"func:{nombre}");
                    Avanzar();

                    if (token != ")")
                    {
                        NodoExpresion primerArg = Expresion();
                        nodoLlamada.Izquierdo = primerArg;
                        NodoExpresion actual = nodoLlamada;

                        while (token == ",")
                        {
                            Avanzar();
                            NodoExpresion argExtra = Expresion();
                            actual.Derecho = new NodoExpresion(",", argExtra, null);
                            actual = actual.Derecho;
                        }
                    }

                    if (token != ")")
                        Error("Falta ')' al cerrar la llamada a función");
                    else
                        Avanzar();

                    return nodoLlamada;
                }

                if (token == "++" || token == "--")
                {
                    string opPostfijo = token;
                    if (!VariableDeclarada(nombre))
                        Error($"La variable '{nombre}' no ha sido declarada");
                    Avanzar();
                    return new NodoExpresion(nombre + opPostfijo);
                }

                if (!VariableDeclarada(nombre) && !ExisteFuncion(nombre))
                    Error($"La variable '{nombre}' no ha sido declarada");

                return new NodoExpresion(nombre);
            }

            if (token == "(")
            {
                Avanzar();
                NodoExpresion inner = Expresion();

                if (token != ")")
                    Error("Falta ')' de cierre en la expresión");
                else
                    Avanzar();

                return new NodoExpresion("()", inner, null);
            }

            Error($"Se esperaba un operando, se encontró '{token}'");
            return new NodoExpresion("?");
        }

        private NodoExpresion Expresion()
        {
            NodoExpresion izq = Operando();

            while (EsOperador())
            {
                string op = token;
                Avanzar();

                NodoExpresion der = Operando();
                izq = new NodoExpresion(op, izq, der);
            }

            return izq;
        }

        private NodoExpresion AnalizarExpresion()
        {
            if (!EsInicioOperando())
            {
                Error($"Se esperaba una expresión, se encontró '{token}'");
                return new NodoExpresion("?");
            }

            NodoExpresion raiz = Expresion();
            return raiz;

        }

        private char Tipo_caracter(int caracter)
        {
            if ((caracter >= 65 && caracter <= 90) || (caracter >= 97 && caracter <= 122)) return 'l';
            if (caracter >= 48 && caracter <= 57) return 'd';

            switch (caracter)
            {
                case 10: return 'n';
                case 34: return '"';
                case 39: return 'c';
                case 32: return 'e';
                case 47: return '/';
                default: return 's';
            }
        }

        private void Simbolo()
        {
            char ch = (char)i_caracter;
            elemento = ch.ToString() + "\n";

            bool esValido =
                i_caracter == '!' ||
                (i_caracter >= 35 && i_caracter <= 38) ||
                (i_caracter >= 40 && i_caracter <= 45) ||
                i_caracter == 47 ||
                (i_caracter >= 58 && i_caracter <= 62) ||
                i_caracter == 91 || i_caracter == 93 ||
                i_caracter == 94 || i_caracter == 123 ||
                i_caracter == 124 || i_caracter == 125 ||
                i_caracter == ',' || i_caracter == ';' ||
                i_caracter == '<' || i_caracter == '>' || i_caracter == '=';

            if (!esValido)
                Error($"Símbolo inesperado '{ch}'");
        }

        private void Cadena()
        {
            do
            {
                i_caracter = Leer.Read();
                if (i_caracter == 10) Numero_linea++;
            } while (i_caracter != 34 && i_caracter != -1);

            if (i_caracter == -1) Error(-1);
        }

        private void Caracter()
        {
            i_caracter = Leer.Read();
            if (i_caracter == -1) { Error("Carácter incompleto"); return; }
            i_caracter = Leer.Read();
            if (i_caracter != 39) Error(39);
        }

        private void Archivo_Libreria()
        {
            i_caracter = Leer.Read();
            if ((char)i_caracter == 'h')
            {
                Escribir.Write("libreria\n");
                i_caracter = Leer.Read();
            }
            else
            {
                Error(i_caracter);
            }
        }

        private bool Palabra_Reservada()
        {
            return P_Reservadas.IndexOf(elemento.ToLower()) >= 0;
        }

        private void Identificador()
        {
            elemento = "";
            do
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Archivo_Libreria();
            }
            else if (Palabra_Reservada())
            {
                Escribir.Write(elemento.ToLower() + "\n");
            }
            else
            {
                Escribir.Write("identificador\n");
                Escribir.Write(elemento + "\n");
            }
        }

        private void Numero_Real(string parteEntera)
        {
            string valorAcum = parteEntera + ".";
            i_caracter = Leer.Read();
            while (Tipo_caracter(i_caracter) == 'd')
            {
                valorAcum += (char)i_caracter;
                i_caracter = Leer.Read();
            }
            Escribir.Write("numero_real\n");
            Escribir.Write(valorAcum + "\n");
        }

        private void Numero()
        {
            string valorAcum = "";
            while (Tipo_caracter(i_caracter) == 'd')
            {
                valorAcum += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            if ((char)i_caracter == '.')
                Numero_Real(valorAcum);
            else
            {
                Escribir.Write("numero\n");
                Escribir.Write(valorAcum + "\n");
            }
        }

        private bool Comentario()
        {
            int siguiente = Leer.Peek();

            if (siguiente == 47)
            {
                Leer.Read();
                do { i_caracter = Leer.Read(); } while (i_caracter != 10 && i_caracter != -1);
                return true;
            }
            else if (siguiente == 42)
            {
                Leer.Read();
                do
                {
                    do
                    {
                        i_caracter = Leer.Read();
                        if (i_caracter == 10) Numero_linea++;
                    } while (i_caracter != 42 && i_caracter != -1);

                    i_caracter = Leer.Read();
                } while (i_caracter != 47 && i_caracter != -1);

                if (i_caracter == -1) Error(i_caracter);
                i_caracter = Leer.Read();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Avanzar()
        {
            token = Leer.ReadLine()?.Trim();

            if (token == "LF")
            {
                Numero_linea++;
                Avanzar();
                return;
            }

            if (token == "identificador")
                valorToken = Leer.ReadLine()?.Trim();

            if (token == "numero" || token == "numero_real")
            {
                string raw = Leer.ReadLine()?.Trim();
                double.TryParse(raw,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out valorNumericoToken);
            }
        }

        private void AnalizadorSintactico()
        {
            Numero_linea = 1;
            Leer = new StreamReader(archivoback);

            TablaFunciones.Clear();
            PilaAmbitos.Clear();
            direccionActual = 0;

            TablaFunciones.Add(new SimboloFuncion("printf", "int"));
            EntrarAmbito();

            Avanzar();
            Cabecera();
            Leer.Close();
        }

        private void Cabecera()
        {
            while (token != null && token != "Fin")
            {
                switch (token)
                {
                    case "#":
                        Avanzar();
                        if (token == "include" || token == "define")
                            Directiva_proc();
                        else
                        {
                            Error("Error en directiva");
                            Avanzar();
                        }
                        break;

                    case "LF":
                        Numero_linea++;
                        Avanzar();
                        break;

                    case "int":
                    case "float":
                    case "double":
                    case "char":
                    case "void":
                        Declaracion();
                        break;

                    case "main":
                        Error("La función 'main' debe tener un tipo de retorno (ej. int main)");
                        Procesar_Main();
                        break;

                    default:
                        if (token != null && token != "Fin")
                        {
                            Error($"Token inesperado en cabecera: '{token}'");
                            Avanzar();
                        }
                        break;
                }
            }
        }

        private int Directiva_proc()
        {
            if (token == "include")
            {
                Avanzar();

                if (token == "<")
                {
                    Avanzar();

                    if (token == ">")
                    {
                        Error("Se esperaba el nombre de la librería entre '<' y '>'");
                        Avanzar();
                        return 0;
                    }

                    if (token == "Fin" || token == "LF")
                    {
                        Error("Falta '>' al final del include");
                        return 0;
                    }

                    bool tieneLibreria = false;
                    while (token != ">" && token != "Fin" && token != "LF")
                    {
                        if (token == "libreria") tieneLibreria = true;
                        Avanzar();
                    }

                    if (token != ">")
                    {
                        Error("Falta '>' al final del include");
                        return 0;
                    }

                    if (!tieneLibreria)
                        Error("No se encontró una librería válida en el include");

                    Avanzar();
                    return 1;
                }
                else if (token == "Cadena")
                {
                    Avanzar();
                    return 1;
                }
                else
                {
                    Error("Se esperaba '<libreria>' o \"archivo\" después de include");
                    return 0;
                }
            }
            else if (token == "define")
            {
                Avanzar();
                while (token != "LF" && token != "Fin") Avanzar();
                return 1;
            }

            return 0;
        }

        private void Declaracion()
        {
            tipoActual = token;
            Avanzar();

            if (token == null) { Error("Declaración incompleta"); return; }
            if (token == "main") { Procesar_Main(); return; }

            if (token != "identificador")
            {
                Error("Se esperaba un identificador después del tipo de dato");
                Avanzar();
                return;
            }

            string nombreVariable = valorToken;
            Avanzar();

            if (token == null) { Error("Declaración incompleta"); return; }

            switch (token)
            {
                case ";":
                    if (ExisteVariableEnAmbitoActual(nombreVariable))
                        Error($"La variable '{nombreVariable}' ya fue declarada en este ámbito");
                    else
                        AgregarVariable(nombreVariable);
                    Avanzar();
                    break;

                case "=":
                    if (ExisteVariableEnAmbitoActual(nombreVariable))
                        Error($"La variable '{nombreVariable}' ya fue declarada en este ámbito");
                    else
                        AgregarVariable(nombreVariable);
                    Dec_VGlobal();
                    break;

                case "[":
                    if (ExisteVariableEnAmbitoActual(nombreVariable))
                        Error($"La variable '{nombreVariable}' ya fue declarada en este ámbito");
                    else
                        AgregarVariable(nombreVariable);
                    D_Arreglos();
                    break;

                case "(":
                    Definicion_Funcion(nombreVariable);
                    break;

                default:
                    Error($"Se esperaba ';', '=', '[' o '(' pero se encontró '{token}'");
                    Avanzar();
                    break;
            }
        }

        private void Dec_VGlobal()
        {
            Avanzar();

            if (token == null) { Error("Inicialización incompleta después de '='"); return; }

            if (EsInicioOperando())
            {
                NodoExpresion raiz = AnalizarExpresion();
                ImprimirArbol(raiz);

                while (token == "LF") Avanzar();

                if (token == ";")
                    Avanzar();
                else
                    Error(token, ";");
            }
            else if (token == "[")
            {
                D_Arreglos();
            }
            else if (token == ";")
            {
                Error("Falta expresión después de '=' en la inicialización");
                Avanzar();
            }
            else
            {
                Error("Token inesperado en inicialización global: " + token);
                while (token != null && token != ";" && token != "Fin")
                    token = Leer.ReadLine();
                if (token == ";") Avanzar();
            }
        }

        private int Constante()
        {
            switch (token)
            {
                case "-":
                    token = Leer.ReadLine();
                    return (token == "numero_real" || token == "numero" || token == "identificador") ? 1 : 0;
                case "numero_real":
                case "numero":
                case "caracter":
                case "identificador":
                    return 1;
                default:
                    return 0;
            }
        }

        private void D_Arreglos()
        {
            Avanzar();

            while (true)
            {
                if (token != "numero" && token != "identificador")
                {
                    Error("Se esperaba tamaño del arreglo");
                    return;
                }
                Avanzar();

                if (token != "]") { Error("Falta ']'"); return; }
                Avanzar();

                if (token == "[") { Avanzar(); continue; }
                break;
            }

            if (token == "=")
            {
                Avanzar();
                if (token != "{") { Error("Se esperaba '{' para iniciar la inicialización"); return; }

                int balance = 1;
                bool primerElemento = true;
                Avanzar();

                while (balance > 0 && token != "Fin" && token != ";")
                {
                    if (token == "{")
                    {
                        if (!primerElemento) Error("Falta ',' entre sub-arreglos");
                        balance++;
                        primerElemento = true;
                        Avanzar();
                    }
                    else if (token == "}")
                    {
                        balance--;
                        primerElemento = false;
                        Avanzar();
                    }
                    else if (token == ",")
                    {
                        if (primerElemento) Error("Coma inesperada al inicio de bloque");
                        primerElemento = true;
                        Avanzar();
                    }
                    else if (token == "numero" || token == "identificador" ||
                             token == "numero_real" || token == "caracter")
                    {
                        if (!primerElemento) Error("Falta ',' entre valores");
                        primerElemento = false;
                        Avanzar();
                    }
                    else
                    {
                        Error("Token inesperado en inicialización: " + token);
                        Avanzar();
                    }
                }

                if (balance > 0) Error("Falta '}' de cierre en la inicialización");
            }

            if (token != ";")
                Error("Falta ';' al final del arreglo");
            else
                Avanzar();
        }

        private void Bloque_Codigo(bool crearAmbito = true)
        {
            if (crearAmbito) EntrarAmbito();
            Avanzar();

            while (token != "}" && token != "Fin" && token != null)
            {
                switch (token)
                {
                    case ";":
                        Avanzar();
                        break;

                    case "=":
                        Error("Asignación inválida: falta variable del lado izquierdo");
                        Avanzar();
                        break;

                    case "int":
                    case "float":
                    case "double":
                    case "char":
                    case "bool":
                        Declaracion();
                        break;

                    case "if": Estructura_If(); break;
                    case "while": Estructura_While(); break;
                    case "for": Estructura_For(); break;
                    case "switch": Estructura_Switch(); break;

                    case "else":
                        Error("Error de sintaxis: Se encontró un 'else' inesperado. Posiblemente falta una llave de cierre '}' en el bloque anterior.");
                        return;

                    case "return":
                        Avanzar();
                        if (EsInicioOperando())
                        {
                            NodoExpresion raizReturn = AnalizarExpresion();
                            ImprimirArbol(raizReturn);
                        }
                        if (token == ";") Avanzar();
                        else Error("Falta ';' después del return");
                        break;

                    case "++":
                    case "--":
                        AnalizarExpresion();
                        if (token == ";") Avanzar();
                        break;

                    case "identificador":
                        string nombreUso = valorToken;
                        Avanzar();

                        if (token == "=")
                        {
                            if (!VariableDeclarada(nombreUso))
                                Error($"La variable '{nombreUso}' no ha sido declarada");

                            Avanzar();

                            if (EsInicioOperando())
                            {
                                NodoExpresion raizAsig = AnalizarExpresion();
                                ImprimirArbol(raizAsig);
                            }
                            else
                                Error("Se esperaba una expresión después de '='");

                            if (token == ";")
                                Avanzar();
                            else
                                Error("Falta ';' al final de la asignación");

                            continue;
                        }
                        else if (token == "(")
                        {
                            if (!ExisteFuncion(nombreUso))
                                Error($"La función '{nombreUso}' no ha sido declarada");

                            NodoExpresion nodoLlamada = new NodoExpresion($"func:{nombreUso}");
                            Avanzar();

                            if (token != ")")
                            {
                                NodoExpresion primerArg = Expresion();
                                nodoLlamada.Izquierdo = primerArg;
                                NodoExpresion actual = nodoLlamada;

                                while (token == ",")
                                {
                                    Avanzar();
                                    NodoExpresion argExtra = Expresion();
                                    actual.Derecho = new NodoExpresion(",", argExtra, null);
                                    actual = actual.Derecho;
                                }
                            }

                            if (token == ")")
                                Avanzar();
                            else
                                Error("Falta ')' al cerrar la llamada a función");

                            if (token == ";") Avanzar();
                        }
                        else if (token == "++" || token == "--")
                        {
                            if (!VariableDeclarada(nombreUso))
                                Error($"La variable '{nombreUso}' no ha sido declarada");

                            Avanzar();

                            if (token == ";") Avanzar();
                        }
                        else
                        {
                            if (!VariableDeclarada(nombreUso))
                                Error($"La variable '{nombreUso}' no ha sido declarada");

                            while (token != ";" && token != "Fin")
                            {
                                if (token == "identificador")
                                {
                                    string subUso = valorToken;
                                    if (!VariableDeclarada(subUso))
                                        Error($"La variable '{subUso}' no ha sido declarada");
                                }
                                Avanzar();
                            }

                            if (token == ";") Avanzar();
                        }
                        break;

                    case "LF":
                        Avanzar();
                        break;

                    default:
                        if (token != "LF" && token != "Fin")
                        {
                            if (token == "+" || token == "-" || token == "*" || token == "/")
                                Avanzar();
                            else
                            {
                                Error("Token no reconocido en bloque: " + token);
                                Avanzar();
                            }
                        }
                        break;
                }
            }

            if (token == "}")
            {
                if (crearAmbito) SalirAmbito();
                Avanzar();
            }
            else if (token == "Fin")
            {
                Error("Falta la llave de cierre '}' al final del bloque");
            }
        }

        private void Validar_Expresion_Parentesis()
        {
            Avanzar();

            if (EsInicioOperando())
            {
                NodoExpresion nodo = Expresion();
            }

            if (token != ")")
                Error("Paréntesis no balanceados en la expresión");
            else
                Avanzar();
        }

        private void Estructura_If()
        {
            Avanzar();
            if (token != "(") Error("Se esperaba '(' después de 'if'");

            Validar_Expresion_Parentesis();

            if (token == "{")
            {
                Bloque_Codigo();
            }
            else
            {
                Error("Se esperaba '{' después de la condición del if");
                while (token != ";" && token != "Fin") Avanzar();
                if (token == ";") Avanzar();
            }

            if (token == "else")
            {
                Avanzar();
                if (token == "{")
                    Bloque_Codigo();
                else if (token == "if")
                    Estructura_If();
                else
                    Error("Se esperaba '{' o 'if' después de 'else'");
            }
        }

        private void Estructura_While()
        {
            Avanzar();
            if (token != "(") Error("Se esperaba '(' después de 'while'");

            Validar_Expresion_Parentesis();

            if (token != "{") Error("Se esperaba '{' después de la condición del while");

            Bloque_Codigo();
        }

        private void Estructura_For()
        {
            Avanzar();
            if (token != "(") Error("Se esperaba '(' después de 'for'");
            Avanzar();

            while (token != ";" && token != "Fin") Avanzar();
            if (token != ";") Error("Falta ';' en la inicialización del for");
            Avanzar();

            while (token != ";" && token != "Fin") Avanzar();
            if (token != ";") Error("Falta ';' en la condición del for");
            Avanzar();

            while (token != ")" && token != "Fin") Avanzar();
            if (token != ")") Error("Se esperaba ')' al finalizar el for");
            Avanzar();

            if (token != "{") Error("Se esperaba '{' después de los parámetros del for");

            Bloque_Codigo();
        }

        private void Estructura_Switch()
        {
            Avanzar();
            if (token != "(") Error("Se esperaba '(' después de 'switch'");
            Validar_Expresion_Parentesis();

            if (token != "{") Error("Se esperaba '{' para abrir el switch");
            Avanzar();

            while (token != "}" && token != "Fin")
            {
                if (token == "case")
                {
                    Avanzar();
                    if (token == "numero" || token == "caracter" || token == "identificador")
                        Avanzar();
                    else
                        Error("Se esperaba un valor constante para el 'case'");

                    if (token != ":") Error("Se esperaba ':' después del valor del case");
                    Avanzar();

                    while (token != "break" && token != "case" && token != "default" &&
                           token != "}" && token != "Fin")
                    {
                        Avanzar();
                        if (token == ";") Avanzar();
                    }

                    if (token == "break")
                    {
                        Avanzar();
                        if (token == ";") Avanzar();
                        else Error("Falta ';' después del break");
                    }
                }
                else if (token == "default")
                {
                    Avanzar();
                    if (token != ":") Error("Se esperaba ':' después de default");
                    Avanzar();
                    while (token != "}" && token != "break" && token != "Fin") Avanzar();
                    if (token == "break")
                    {
                        Avanzar();
                        if (token == ";") Avanzar();
                    }
                }
                else if (token == "LF")
                {
                    Avanzar();
                }
                else
                {
                    Avanzar();
                }
            }

            if (token == "}") Avanzar();
        }

        private void Procesar_Main()
        {
            Avanzar();

            if (token != "(")
                Error("Falta '(' después de main");
            else
                Avanzar();

            if (token != ")")
            {
                Error("Falta ')' en main");
                while (token != ")" && token != "{" && token != "Fin") Avanzar();
                if (token == ")") Avanzar();
            }
            else
            {
                Avanzar();
            }

            if (token == "{")
                Bloque_Codigo();
            else
                Error("Falta '{' para iniciar el cuerpo del main");
        }

        private void Definicion_Funcion(string nombreFuncion)
        {
            if (ExisteFuncion(nombreFuncion))
                Error($"La función '{nombreFuncion}' ya fue declarada");

            SimboloFuncion funcion = new SimboloFuncion(nombreFuncion, tipoActual);
            Avanzar();

            if (token == ")")
            {
                Avanzar();
            }
            else
            {
                while (true)
                {
                    if (token != "int" && token != "float" && token != "double" &&
                        token != "char" && token != "void")
                    {
                        Error("Se esperaba un tipo de dato en los parámetros");
                        return;
                    }

                    string tipoParametro = token;
                    funcion.TiposParametros.Add(tipoParametro);
                    funcion.NumeroParametros++;
                    Avanzar();

                    if (token != "identificador")
                    {
                        Error("Se esperaba un identificador como nombre del parámetro");
                        return;
                    }

                    string nombreParametro = valorToken;

                    if (ExisteVariableEnAmbitoActual(nombreParametro))
                    {
                        Error($"El parámetro '{nombreParametro}' ya fue declarado");
                    }
                    else
                    {
                        PilaAmbitos.Peek().Add(new SimboloVariable(nombreParametro, tipoParametro, direccionActual));
                        direccionActual += 4;
                    }

                    Avanzar();

                    if (token == ",")
                    {
                        Avanzar();
                        continue;
                    }
                    else if (token == ")")
                    {
                        Avanzar();
                        break;
                    }
                    else
                    {
                        Error("Se esperaba ',' o ')'");
                        return;
                    }
                }
            }

            if (token == "{")
            {
                TablaFunciones.Add(funcion);
                Bloque_Codigo();
            }
            else
            {
                Error("Se esperaba '{' para iniciar el cuerpo de la función");
            }
        }

        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox2.Text = "";
            guardar();

            elemento = "";
            N_error = 0;
            Numero_linea = 1;

            if (archivo == null) { MessageBox.Show("No hay archivo cargado."); return; }

            archivoback = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoback);
            Leer = new StreamReader(archivo);

            i_caracter = Leer.Read();

            do
            {
                elemento = "";

                switch (Tipo_caracter(i_caracter))
                {
                    case 'l': Identificador(); break;
                    case 'd': Numero(); break;
                    case 's':
                        if (i_caracter == '=' || i_caracter == '!' ||
                            i_caracter == '<' || i_caracter == '>' ||
                            i_caracter == '+' || i_caracter == '-' ||
                            i_caracter == '&' || i_caracter == '|')
                        {
                            int primero = i_caracter;
                            int segundo = Leer.Peek();

                            if ((primero == '=' && segundo == '=') ||
                                (primero == '!' && segundo == '=') ||
                                (primero == '<' && segundo == '=') ||
                                (primero == '>' && segundo == '=') ||
                                (primero == '+' && segundo == '+') ||
                                (primero == '-' && segundo == '-') ||
                                (primero == '&' && segundo == '&') ||
                                (primero == '|' && segundo == '|'))
                            {
                                Leer.Read();
                                string doble = ((char)primero).ToString() + ((char)segundo).ToString();
                                Escribir.Write(doble + "\n");
                                i_caracter = Leer.Read();
                            }
                            else
                            {
                                Simbolo();
                                Escribir.Write(elemento);
                                i_caracter = Leer.Read();
                            }
                        }
                        else
                        {
                            Simbolo();
                            Escribir.Write(elemento);
                            i_caracter = Leer.Read();
                        }
                        break;
                    case '/':
                        if (Comentario())
                        {
                            Escribir.Write("Comentario\n");
                        }
                        else
                        {
                            Escribir.Write("/\n");
                            i_caracter = Leer.Read();
                        }
                        break;
                    case '"':
                        Cadena();
                        Escribir.Write("Cadena\n");
                        i_caracter = Leer.Read();
                        break;
                    case 'c':
                        Caracter();
                        Escribir.Write("Caracter\n");
                        i_caracter = Leer.Read();
                        break;
                    case 'n':
                        i_caracter = Leer.Read();
                        Numero_linea++;
                        Escribir.Write("LF\n");
                        break;
                    case 'e':
                        i_caracter = Leer.Read();
                        break;
                    default:
                        if (i_caracter == '.')
                        {
                            int siguiente = Leer.Peek();
                            if (siguiente >= '0' && siguiente <= '9')
                            {
                                string valPunto = ".";
                                i_caracter = Leer.Read();
                                while (Tipo_caracter(i_caracter) == 'd')
                                {
                                    valPunto += (char)i_caracter;
                                    i_caracter = Leer.Read();
                                }
                                Escribir.Write("numero_real\n");
                                Escribir.Write(valPunto + "\n");
                            }
                        }
                        else
                        {
                            Error(i_caracter);
                        }
                        break;
                }
            } while (i_caracter != -1);

            Escribir.Write("Fin\n");
            richTextBox2.AppendText("Errores: " + N_error + "\n");
            Escribir.Close();
            Leer.Close();
            AnalizadorSintactico();
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader LeerF = new StreamReader(archivo))
                {
                    richTextBox1.Text = LeerF.ReadToEnd();
                }
            }
            this.Text = "Mi Compilador - " + archivo;
            analizarToolStripMenuItem.Enabled = true;
        }

        private void guardar()
        {
            if (archivo != null)
            {
                using (StreamWriter EscribirF = new StreamWriter(archivo))
                {
                    EscribirF.Write(richTextBox1.Text);
                }
            }
            else
            {
                SaveFileDialog VentanaGuardar = new SaveFileDialog();
                VentanaGuardar.Filter = "Texto|*.c";
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter EscribirF = new StreamWriter(archivo))
                    {
                        EscribirF.Write(richTextBox1.Text);
                    }
                }
            }
            this.Text = "Mi Compilador - " + archivo;
        }

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e) => guardar();

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            archivo = null;
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (VentanaGuardar.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaGuardar.FileName;
                using (StreamWriter EscribirF = new StreamWriter(archivo))
                {
                    EscribirF.Write(richTextBox1.Text);
                }
            }
            this.Text = "Mi Compilador - " + archivo;
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult Salir = MessageBox.Show(
                "¿Estás seguro que deseas salir?",
                "Confirmar salida",
                MessageBoxButtons.OKCancel);

            if (Salir == DialogResult.OK)
                Application.Exit();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            analizarToolStripMenuItem.Enabled = true;
        }

        private void Error(int i_caracter)
        {
            richTextBox2.AppendText($"Error léxico {(char)i_caracter}, línea {Numero_linea}\n");
            N_error++;
            i_caracter = Leer.Read();
        }

        private void Error(string mensaje)
        {
            richTextBox2.AppendText($"Error: {mensaje}, línea {Numero_linea}\n");
            N_error++;
        }

        private void Error(string tokenLocal, string esperado)
        {
            richTextBox2.AppendText($"Error: se esperaba '{esperado}', pero se encontró '{tokenLocal}', línea {Numero_linea}\n");
            N_error++;
        }
    }
}