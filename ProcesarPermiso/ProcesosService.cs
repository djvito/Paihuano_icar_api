using iTextSharp.text;
using iTextSharp.text.pdf;
using App.WindowsService.Datos;
using App.WindowsService.Utiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.WindowsService.Models;
using Newtonsoft.Json;
using System.Net;
using System.Xml;

namespace ProcesarPermiso
{
    public class ProcesosService
    {
        private readonly PermisosCirculacionServicio _permisosCirculacionServicio;
        private readonly PermisosCirculacionSolicitudServicio _permisosCirculacionSolicitudServicio;
        private readonly PermisosCirculacionEnvioServicio _permisosCirculacionEnvioServicio;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProcesosService> _logger;
        private static int maxPermisosDefault = 100;
        private static IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, true)
            .Build();

        public ProcesosService(PermisosCirculacionServicio permisosCirculacionServicio, PermisosCirculacionSolicitudServicio permisosCirculacionSolicitudServicio, PermisosCirculacionEnvioServicio permisosCirculacionEnvioServicio, HttpClient httpClient, ILogger<ProcesosService> logger)
        {
            _permisosCirculacionServicio = permisosCirculacionServicio;
            _permisosCirculacionSolicitudServicio = permisosCirculacionSolicitudServicio;
            _permisosCirculacionEnvioServicio = permisosCirculacionEnvioServicio;
            _httpClient = httpClient;
            _logger = logger;
        }

        private static int MaxPermisosPorProcesoSolicitud()
        {
            int colaMaxRegistrosPorProceso = 0;

            try
            {
                colaMaxRegistrosPorProceso = config.GetSection("ColaSolicitud").GetValue<int>("MaxPermisosPorProceso");
            }
            catch (Exception ex) { }

            if (colaMaxRegistrosPorProceso == 0)
            {
                colaMaxRegistrosPorProceso = maxPermisosDefault;
            }

            return colaMaxRegistrosPorProceso;
        }

        private static int MaxPermisosPorProcesoEnvio()
        {
            int colaMaxRegistrosPorProceso = 0;

            try
            {
                colaMaxRegistrosPorProceso = config.GetSection("ColaEnvio").GetValue<int>("MaxPermisosPorProceso");
            }
            catch (Exception ex) { }

            if (colaMaxRegistrosPorProceso == 0)
            {
                colaMaxRegistrosPorProceso = maxPermisosDefault;
            }

            return colaMaxRegistrosPorProceso;
        }

        public void ColaCrearPermisos()//primer proceso
        {
            var solicitudes = _permisosCirculacionSolicitudServicio.SelTop(MaxPermisosPorProcesoSolicitud());
            int procesados = 0;

            if (solicitudes.Count() == 0)
            {
                return;
            }

            try
            {
                string archivoPdfTimbre = config.GetSection("GenerarPDF").GetValue<string>("ArchivoPdfTimbre");
                string archivoPdfFondo = config.GetSection("GenerarPDF").GetValue<string>("ArchivoPdfFondo");
                string pdfCarpetaPDF = config.GetSection("GenerarPDF").GetValue<string>("CarpetaPDF");

                string CarpetaXML = config.GetSection("GenerarPDF").GetValue<string>("CarpetaXML");

                if (!File.Exists(archivoPdfTimbre))
                {
                    _logger.LogError($"No existe el archivo {archivoPdfTimbre}, proceso abortado...");
                    return;
                }

                if (!File.Exists(archivoPdfFondo))
                {
                    _logger.LogError($"No existe el archivo {archivoPdfFondo}, proceso abortado...");
                    return;
                }

                if (!Directory.Exists(pdfCarpetaPDF))
                {
                    try
                    {
                        Directory.CreateDirectory(pdfCarpetaPDF);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al crear la carpeta {pdfCarpetaPDF} {ex}");
                    }

                    if (!Directory.Exists(pdfCarpetaPDF))
                    {
                        _logger.LogError($"La carpate {pdfCarpetaPDF} no existe ni fue posible crearla, proceso abortado...");
                        return;
                    }
                }

                foreach (var solicitud in solicitudes)
                {
                    PermisoCirculacionInsModel permiso = new PermisoCirculacionInsModel()
                    {
                        id_permisosCirculacion = 0,
                        tipo = 5,
                        rut = solicitud.rut ?? "",
                        patente = solicitud.patente ?? "",
                        monto_neto = solicitud.monto_neto ?? "",
                        fecha_vencimiento = solicitud.fecha_vencimiento,
                        digito_verificador = solicitud.digito_verificador ?? "",
                        Marca = solicitud.Marca ?? "",
                        Modelo = solicitud.Modelo ?? "",
                        ano = solicitud.ano ?? "",
                        tipo_vehiculo = solicitud.tipo_vehiculo ?? "",
                        motor = solicitud.motor ?? "",
                        color = solicitud.color ?? "",
                        chasis = solicitud.chasis ?? "",
                        n_puertas = solicitud.n_puertas ?? "",
                        n_asiento = solicitud.n_asiento ?? "",
                        tara = solicitud.tara ?? "",
                        codigo_sii = solicitud.codigo_sii ?? "",
                        tasacion = solicitud.tasacion ?? "",
                        cilindrada = solicitud.cilindrada ?? "",
                        combustible = solicitud.combustible ?? "",
                        transmision = solicitud.transmision ?? "",
                        equipamiento = solicitud.equipamiento ?? "",
                        nombre_propietario = solicitud.nombre_propietario ?? "",
                        domicilio_propietario = solicitud.domicilio_propietario ?? "",
                        comuna_propietario = solicitud.comuna_propietario ?? "",
                        telefono_propietario = solicitud.telefono_propietario ?? "",
                        pago_total = solicitud.pago_total ?? "",
                        sello_verde = solicitud.sello_verde ?? "",
                        comuna_anterior = solicitud.comuna_anterior ?? "",
                        zona_franca = solicitud.zona_franca ?? "",
                        carga = solicitud.carga ?? "",
                        multa = solicitud.multa ?? "",
                        ipc = solicitud.ipc ?? 0,
                        valor_total = solicitud.valor_total ?? 0,
                        interes = solicitud.interes ?? 0,
                        total_neto = solicitud.total_neto ?? 0,
                        total_pagado = solicitud.total_pagado ?? 0,
                        cuota = solicitud.cuota ?? "",
                        empresa = solicitud.empresa ?? "",
                        usuario = solicitud.usuario ?? "",
                        fecha_factura = solicitud.fecha_factura ?? "",
                        neto_factura = solicitud.neto_factura ?? "",
                        iva_factura = solicitud.iva_factura ?? "",
                        total_factura = solicitud.total_factura ?? ""
                    };

                    RespuestaGenericaModel? res = _permisosCirculacionServicio.Ins(permiso);//proceso de insertar datos en BD permiso_circulacion unica tabla

                    if (res.id > 0)
                    {
                        permiso.id_permisosCirculacion = res.id ?? 00;

                        string archivoPdf = GenerarPDF(archivoPdfTimbre, archivoPdfFondo, pdfCarpetaPDF, permiso, res.fecha.ToString("dd-MM-yyyy"));

                        if (archivoPdf == "")
                        {
                            _logger.LogError($"Error al generar PDF, por favor revise los permisos de la carpeta {pdfCarpetaPDF}");
                            _permisosCirculacionServicio.DelPorId(res.id ?? 0);
                            return;
                        }
                        else
                        {
                            var resEnvio = _permisosCirculacionEnvioServicio.Ins(res.id ?? 0, archivoPdf);


                            if (resEnvio.resultado == 1)
                            {
                                procesados++;
                                _permisosCirculacionSolicitudServicio.DelPorId(solicitud.id_permisosCirculacion_solicitud);
                                _logger.LogInformation($"Se borro el Id y se inserto en permisos_circulacion_envio_Icar {solicitud.id_permisosCirculacion_solicitud}");
                            }
                            else
                            {
                                _permisosCirculacionServicio.DelPorId(res.id ?? 0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex}");
            }

            if (procesados > 0)
            {
                if (procesados == 1)
                {
                    _logger.LogInformation($"Se procesó 1 solicitud de Permiso");
                }
                else
                {
                    _logger.LogInformation($"Se procesaron {procesados} solicitudes de Permisos");
                }
            }
        }

        private async Task<PermisoCirculacionApiEnvioRespuestaModel> enviarAsync(string endPoint, string usuario, string password, string placa, string numeroMotor, int codigoResultado, string descripcionResultado, string permiso, long tamanioPermiso, int valorPermiso, int pdf)
        {
            PermisoCirculacionApiEnvioRespuestaModel respuesta = new PermisoCirculacionApiEnvioRespuestaModel();

            string CarpetaXML = config.GetSection("GenerarPDF").GetValue<string>("CarpetaXML");

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var datos = detallesEnvio(usuario,
                        password,
                        placa,
                        numeroMotor,
                        codigoResultado.ToString(),
                        descripcionResultado,
                        "",
                        tamanioPermiso.ToString(),
                        permiso,
                         "1000000",
                        valorPermiso);

                    File.WriteAllText((CarpetaXML + "/Datos_" + placa + ".txt"), datos);
                    _permisosCirculacionEnvioServicio.InsResult(1, 1, pdf, datos);

                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    XmlDocument xmlDoc = new XmlDocument();

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endPoint);

                    byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(datos);
                    req.Method = "POST";
                    req.ContentType = "text/xml;charset=utf-8"; //application/xml
                    req.ContentLength = requestBytes.Length;
                    Stream requestStream = req.GetRequestStream();
                    requestStream.Write(requestBytes, 0, requestBytes.Length);
                    requestStream.Close();

                    HttpWebResponse resHttp = (HttpWebResponse)req.GetResponse();
                    StreamReader sr = new StreamReader(resHttp.GetResponseStream(), System.Text.Encoding.Default);
                    string RespuestaIcar = sr.ReadToEnd();
                    File.WriteAllText((CarpetaXML + "/Respuesta" + placa + ".txt"), RespuestaIcar);
                    _permisosCirculacionEnvioServicio.InsResult(2, 1, pdf, RespuestaIcar);
                    try
                    {
                        respuesta = JsonConvert.DeserializeObject<PermisoCirculacionApiEnvioRespuestaModel>(RespuestaIcar);
                    }
                    catch (Exception ex) { }

                    if (respuesta.MENSAJE_RESPUESTA != null)
                    {
                        return respuesta;
                    }
                    else
                    {
                        xmlDoc.LoadXml(RespuestaIcar);

                        XmlNodeList xnList = xmlDoc.SelectNodes("RESPUESTAPCI");

                        foreach (XmlNode xn in xnList)
                        {
                            respuesta.COD_RESP = xn["COD_RESP"].InnerText;
                            respuesta.DESC_RESP = xn["DESC_RESP"].InnerText;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error {ex}");
                    respuesta.COD_RESP = "7";
                }
            }
            return respuesta;
        }

        public async Task ColaEnvioPermisosAsync() // tercer proceso de envio de cola
        {
            var envios = _permisosCirculacionEnvioServicio.SelTop(MaxPermisosPorProcesoEnvio());
            int procesados = 0;

            if (envios.Count() == 0)
            {
                return;
            }

            try
            {
                string endPoint = config.GetSection("ColaEnvio").GetValue<string>("EndPoint");
                int maxIntentos = config.GetSection("ColaEnvio").GetValue<int>("MaxIntentos");
                string usuario = config.GetSection("ColaEnvio").GetValue<string>("Usuario");
                string password = config.GetSection("ColaEnvio").GetValue<string>("Password");

                foreach (var envio in envios)
                {
                    var nombrepdf = envio.archivo_pdf;
                    var pdf = nombrepdf.Substring(0, 9);
                    long tamanioArchivoPdf = 0;
                    String pdfBase64 = "";

                    try
                    {
                        string pdfCarpetaPDF = config.GetSection("GenerarPDF").GetValue<string>("CarpetaPDF");
                        string archivoPDF = Path.Combine(pdfCarpetaPDF, envio.archivo_pdf);
                        tamanioArchivoPdf = new System.IO.FileInfo(archivoPDF).Length;

                        if (tamanioArchivoPdf > 0)
                        {
                            byte[] bytes = File.ReadAllBytes(archivoPDF);
                            pdfBase64 = Convert.ToBase64String(bytes);
                        }
                    }
                    catch (Exception ex) { }

                    _logger.LogInformation($"Enviando Permiso {envio.id_permisosCirculacion}:\n\n     EndPoint: {endPoint}\n     Usuario: {usuario}\n     Password: {password}\n     Patente: {envio.patente}\n     Motor: {envio.motor}\n     Archivo PDF: {envio.archivo_pdf}\n     Tamaño archivo PDF: {tamanioArchivoPdf}\n     ValorPermiso: {envio.total_pagado} ");

                    var respuesta = await enviarAsync(endPoint, usuario, password, envio.patente, envio.motor, 1, "Pagado", pdfBase64, pdfBase64.Length, envio.total_pagado, int.Parse(pdf));

                    if (respuesta.COD_RESP == "0")
                    {
                        _permisosCirculacionEnvioServicio.DelPorId(envio.id_permisos_circulacion_envio);

                        procesados++;
                    }
                    else
                    {
                        _logger.LogError($"Error al enviar Permiso ID {envio.id_permisos_circulacion_envio}: id_permisos_circulacion : {envio.id_permisosCirculacion}, Codigo_Respuesta : {respuesta.COD_RESP}, Mensajes_respuesta : {respuesta.MENSAJE_RESPUESTA}");
                        _permisosCirculacionEnvioServicio.AgregarIntento(envio.id_permisos_circulacion_envio, maxIntentos);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex}");
            }

            if (procesados > 0)
            {
                if (procesados == 1)
                {
                    _logger.LogInformation($"Se procesó 1 envío");
                }
                else
                {
                    _logger.LogInformation($"Se procesaron {procesados} envíos");
                }
            }
        }

        private string detallesEnvio(string usuario, string clave, string nro_placa, string nro_motor, string cod_resp_im, string desc_resp_im, string comentario_revisor, string tamaño, string doc_permiso, string saldo, int valorPermiso)
        {
            _logger.LogInformation($"Creando XML");

            DateTime now;
            string str = "";
            try
            {
                str = "<?xml version='1.0' encoding='UTF-8'?>";
                str = str + " <Respuesta>";
                str = str + "    <USUARIO>" + usuario + "</USUARIO>";
                str = str + "    <CLAVE>" + clave + "</CLAVE>";
                str = str + "    <PLACA>" + nro_placa + "</PLACA>";
                str = str + "    <NRO_MOTOR>" + nro_motor + "</NRO_MOTOR>";
                str = str + "    <COD_RESP_IM>" + cod_resp_im + "</COD_RESP_IM>";
                str = str + "    <DESC_RESP_IM>" + desc_resp_im + "</DESC_RESP_IM>";
                str = str + "    <COMENTARIO_REVISOR>" + comentario_revisor + "</COMENTARIO_REVISOR>";
                str = str + "    <TAMANO>" + tamaño + "</TAMANO>";
                str = str + "    <DOC_PERMISO>" + doc_permiso + "</DOC_PERMISO>";
                str = str + "    <SALDO>" + saldo + "</SALDO>";
                str = str + "    <VALORPC>" + valorPermiso + "</VALORPC>";
                str = str + "</Respuesta>";
                return str;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex}");
                return str + "Error al generar XML";
            }
        }

        public static string GenerarPDF(string archivoPdfTimbre, string archivoPdfFondo, string carpetaPDF, PermisoCirculacionInsModel permiso, string fechaPermiso)
        {
            string archivoPDF = permiso.id_permisosCirculacion + "_" + permiso.patente + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".PDF";
            string rutaInternaPdf = Path.Combine(carpetaPDF + "\\" + archivoPDF);

            MatchCollection matches;
            Regex regex;

            string rut = Funciones.ValidarRut(permiso.rut);
            string patente = permiso.patente;
            string neto_cuota = permiso.monto_neto;
            string fecha_vencimiento = (permiso.fecha_vencimiento == null ? "" : (permiso.fecha_vencimiento ?? DateTime.Now).ToString("dd-MM-yyyy"));
            string dv_patente = permiso.digito_verificador;
            string marca = permiso.Marca;
            string modelo = permiso.Modelo;
            string anio = permiso.ano;
            string tipo_vehiculo = permiso.tipo_vehiculo;
            string motor = permiso.motor;
            string color = permiso.color;
            string chasis = permiso.chasis;
            string puertas = permiso.n_puertas;
            string asientos = permiso.n_asiento;
            string tara = permiso.tara;
            string codigo_sii = permiso.codigo_sii;
            string tasacion = permiso.tasacion;
            string cilindrada = permiso.cilindrada;
            string combustible = permiso.combustible;
            string transmision = permiso.transmision;
            string equipamiento = permiso.equipamiento;
            string nombre_propietario = permiso.nombre_propietario;
            string domicilio_propietario = permiso.domicilio_propietario;
            string comuna = permiso.comuna_propietario;
            string telefono_propietario = permiso.telefono_propietario;
            string pago_total = permiso.pago_total ?? "0";
            string sello_verde = permiso.sello_verde;
            string comuna_anterior = permiso.comuna_anterior;
            string zona_franca = permiso.zona_franca;
            string carga = permiso.carga;
            string multa = permiso.multa;
            string ipc = permiso.ipc.ToString();
            string total_neto = permiso.total_neto.ToString();
            string total_pagado = permiso.total_pagado.ToString();
            string intereses = permiso.interes.ToString();


            regex = new Regex(@"([A-Z])");
            matches = regex.Matches(patente);
            string LetraPatente = matches.Count.ToString();

            if (LetraPatente == "4")
            {
                patente = patente.Insert(4, ".");
            }
            else if (LetraPatente == "3")
            {
                patente = patente.Insert(3, ".");
            }
            else
            {
                patente = patente.Insert(2, ".");
            }

            // valida si tiene datos antes de cambiar formato

            if (neto_cuota != "")
            {
                try
                {
                    neto_cuota = long.Parse(neto_cuota.Replace(".", "").Replace(",", ".")).ToString("N0");
                }
                catch (Exception ex) { }

            }
            if (tasacion != "")
            {
                tasacion = int.Parse(tasacion).ToString("N0");

            }
            if (pago_total != "")
            {
                pago_total = int.Parse(pago_total).ToString("N0");

            }
            if (ipc != "")
            {
                ipc = int.Parse(ipc).ToString("N0");

            }
            if (total_neto != "")
            {
                total_neto = int.Parse(total_neto).ToString("N0");

            }
            if (total_pagado != "")
            {
                total_pagado = int.Parse(total_pagado).ToString("N0");

            }
            if (intereses != "")
            {
                intereses = int.Parse(intereses).ToString("N0");

            }
            if (carga != "")
            {
                try { carga = int.Parse(carga).ToString("N0"); } catch (Exception ex) { carga = "0"; }
            }
            if (multa != "")
            {
                try { multa = int.Parse(multa).ToString("N0"); } catch (Exception ex) { multa = "0"; }
            }


            Document doc = new Document(PageSize.LETTER, 30f, 30f, 5f, 40f);
            // Indicamos donde vamos a guardar el documento

            PdfWriter writer = null;

            try
            {
                writer = PdfWriter.GetInstance(doc,
                                            new System.IO.FileStream(rutaInternaPdf, System.IO.FileMode.Create));
            }
            catch (Exception ex)
            {
                return "";
            }


            doc.AddTitle("PERMISO CIRCULACION");
            doc.AddCreator("GOSOLUTIONSCHILE");

            doc.Open();
            PdfContentByte cb = writer.DirectContent;

            // Escribimos el encabezamiento en el documento

            iTextSharp.text.Font _tituloFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
            iTextSharp.text.Font _standardFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 7, iTextSharp.text.Font.NORMAL, new BaseColor(82, 100, 205));
            iTextSharp.text.Font _standardFont2 = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
            iTextSharp.text.Font _tituloFont2 = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            iTextSharp.text.Font _IDFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, BaseColor.RED);
            iTextSharp.text.Font _standardFont3 = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 6, iTextSharp.text.Font.NORMAL, new BaseColor(82, 100, 205));

            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));


            //-----------------------------imagen de fondo------------------------------------


            iTextSharp.text.Image imagen = iTextSharp.text.Image.GetInstance(archivoPdfFondo);
            //iTextSharp.text.Image imagen = iTextSharp.text.Image.GetInstance(Path.GetFullPath("Files/Img/Fondo.jpg"));
            imagen.BorderWidth = 0;
            imagen.Alignment = Element.ALIGN_RIGHT;
            float percentage = 150 / imagen.Width;
            imagen.ScalePercent(percentage * 400);
            //imagen.SetAbsolutePosition(25f, doc.PageSize.Height - 290f);
            imagen.SetAbsolutePosition(0f, 10f);
            doc.Add(imagen);

            // Timbre
            iTextSharp.text.Image imagentimbre = iTextSharp.text.Image.GetInstance(archivoPdfTimbre);
            //iTextSharp.text.Image imagentimbre = iTextSharp.text.Image.GetInstance(Path.GetFullPath("Files/Img/timbre.png"));
            imagentimbre.BorderWidth = 0;
            //imagen.Alignment = Element.ALIGN_RIGHT;
            float percentage1 = 150 / imagentimbre.Width;
            imagentimbre.ScalePercent(20);
            imagentimbre.SetAbsolutePosition(435f, doc.PageSize.Height - 290f);
            doc.Add(imagentimbre);




            //-----------------------------imagen fin-----------------------------------------


            //----------------------------------------- Codigo QR

            var url = "PPAI" + permiso.id_permisosCirculacion;

            BarcodeQRCode Qr = new BarcodeQRCode("https://www.gosolutionschile.cl/Validador.aspx?codid=" + url.ToString(), 300, 300, null);
            iTextSharp.text.Image img = Qr.GetImage();
            //img.ScaleToFit(120f, 120f);            
            img.BorderWidthTop = 1f;
            img.BorderWidthRight = 1f;
            img.BorderWidthLeft = 1f;
            img.BorderWidthBottom = 1f;
            img.SetAbsolutePosition(455f, 295f);
            img.ScaleAbsolute(128f, 128f);
            cb.AddImage(img);


            Qr = new BarcodeQRCode("https://www.gosolutionschile.cl/Validador.aspx?codid=" + url.ToString(), 300, 300, null);
            img = Qr.GetImage();
            //img.ScaleToFit(120f, 120f);
            img.BorderWidthTop = 1f;
            img.BorderWidthRight = 1f;
            img.BorderWidthLeft = 1f;
            img.BorderWidthBottom = 1f;
            img.SetAbsolutePosition(455f, 95f);
            img.ScaleAbsolute(128f, 128f);
            //img.BorderColor = BaseColor.BLUE;
            cb.AddImage(img);


            //------------------------------------------posicion validar documento--------------------------------------------
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Consulte en  https://gosolutionschile.cl/Validador.aspx - Código de validación: " + url, 315f, 285f, 0);
            cb.EndText();


            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Consulte en https://gosolutionschile.cl/Validador.aspx - Código de validación: " + url, 315f, 85f, 0);
            cb.EndText();


            //------------------------------------------ se dibuja las lineas-------------------------------------------------

            //rectangulo permiso 1
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.RoundRectangle(doc.LeftMargin - 5f, doc.PageSize.Height - 295f, doc.PageSize.Width - doc.LeftMargin * 2 + 10f, 155f, 3f);
            cb.Stroke();

            //rectangulo permiso 2
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.RoundRectangle(doc.LeftMargin - 5f, doc.PageSize.Height - 500f, (doc.PageSize.Width - doc.LeftMargin * 2) / 2, 134f, 3f);
            cb.Stroke();

            //rectangulo permiso 3
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.RoundRectangle((doc.PageSize.Width / 2) + 3f, doc.PageSize.Height - 500f, (doc.PageSize.Width - doc.LeftMargin * 2) / 2, 134f, 3f);
            cb.Stroke();

            //rectangulo permiso 4
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.RoundRectangle(doc.LeftMargin - 5f, doc.PageSize.Height - 700f, (doc.PageSize.Width - doc.LeftMargin * 2) / 2, 134f, 3f);
            cb.Stroke();

            //rectangulo permiso 5
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.RoundRectangle((doc.PageSize.Width / 2) + 3f, doc.PageSize.Height - 700f, (doc.PageSize.Width - doc.LeftMargin * 2) / 2, 134f, 3f);
            cb.Stroke();

            //--------------------------------- cuota total cuadrado--------------------------------------

            cb.SaveState();
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(550f, 575f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 2
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(550f, 540f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 3
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(550f, 505f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 4
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(316f, 295f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 5
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(368f, 295f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 6
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(420f, 295f, 20f, 20f, 3f);
            cb.FillStroke();

            //cuadrado pagado 7
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(316f, 95f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 8
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(368f, 95f, 20f, 20f, 3f);
            cb.FillStroke();


            //cuadrado pagado 9
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);
            cb.SetRGBColorFill(230, 230, 230);
            cb.RoundRectangle(420f, 95f, 20f, 20f, 3f);
            cb.FillStroke();
            cb.RestoreState();


            //---------------------------------------------marca X segun pago------------------------------

            // 1 copia
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 18f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "X", 554, 579f, 0);
            cb.EndText();

            //copia 2
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 18f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "X", 424f, 298f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 18f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "X", 424f, 98f, 0);
            cb.EndText();


            //------------------------------ se firman las lineas entre cortadas

            //1 linea 
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.MoveTo(doc.LeftMargin, doc.PageSize.Height - 335f);
            cb.LineTo(doc.PageSize.Width - doc.LeftMargin, doc.PageSize.Height - 335f);
            cb.SetLineDash(2, 1);
            cb.Stroke();

            //2 linea 
            cb.SetLineWidth(0.5);
            cb.SetColorStroke(BaseColor.BLACK);

            cb.MoveTo(doc.LeftMargin, doc.PageSize.Height - 535f);
            cb.LineTo(doc.PageSize.Width - doc.LeftMargin, doc.PageSize.Height - 535f);
            cb.SetLineDash(2, 1);
            cb.Stroke();

            //--------------------------------------------posicion firma electronica texto-----------------------------------------------------
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Este documento contiene una firma electrónica avanzada", 70f, 285f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Este documento contiene una firma electrónica avanzada", 70f, 85f, 0);
            cb.EndText();

            //--------------------------------------------dominio del vehiculo texto-----------------------------------------------------
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "No acredita dominio del vehículo", 110f, 295f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "No acredita dominio del vehículo", 110f, 95f, 0);
            cb.EndText();

            //------------------------------------------posicion absoluta pago total--------------------------------------------------
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL", 550f, 600f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 1", 550f, 565f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 2", 550f, 530f, 0);
            cb.EndText();

            //----------------------------------------- posicion absoluta contribuyente--------------------------------------------
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "1. CONTRIBUYENTE", 23f, 550f, 90);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "2. CONTRIBUYENTE", 23f, 330f, 90);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "3. CONTRIBUYENTE", 23f, 130f, 90);
            cb.EndText();


            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("\n"));

            //---------------------------------------------1 tabla--------------------------------------------------------------

            //TITULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COMPROBANTE DE PAGO DE PERMISO DE CIRCULACION", 32f, 655f, 0);
            cb.EndText();

            //N° SERIE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "N° Serie", 420f, 655f, 0);
            cb.EndText();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(482f, 654f, 100f, 10f, 2f);
            cb.Fill();
            cb.RestoreState();

            cb.SaveState();
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.SetColorFill(new BaseColor(255, 0, 0));
            cb.ShowTextAligned(Element.ALIGN_LEFT, permiso.id_permisosCirculacion.ToString(), 530f, 655f, 0);
            cb.EndText();
            cb.RestoreState();

            //------------------------------------------------------------------------------------------------------------

            //Nombre municipalidad

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(30f, 639f, 552f, 12f, 2f);
            cb.Fill();
            cb.RestoreState();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "I. Municipalidad de Paihuano", 32f, 641f, 0);
            cb.EndText();


            //VALIDA HASTA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "FECHA", 315f, 641f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, Funciones.fechaValida(fecha_vencimiento), 345f, 641f, 0);
            cb.EndText();

            //PLACA UNICA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PLACA UNICA", 435f, 641f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 12f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, patente + "-" + dv_patente, 485f, 641f, 0);
            cb.EndText();

            //--------------------------------------------------------------- Definimos la 2º linea

            //NOMBRE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "NOMBRE (O RAZON SOCIAL)", 32f, 629f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, nombre_propietario, 125f, 629f, 0);
            cb.EndText();

            //RUT
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "RUT", 315f, 629f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, rut, 345f, 629f, 0);
            cb.EndText();


            //FONO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "FONO", 435f, 629f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 465f, 629f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 3º linea

            //DOMICILIO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "DOMICILIO", 32f, 617f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, domicilio_propietario, 75f, 617f, 0);
            cb.EndText();


            //COMUNA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COMUNA", 315f, 617f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, comuna, 345f, 617f, 0);
            cb.EndText();

            //FECHA EMISION
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "FECHA EMISION", 435f, 617f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, fechaPermiso, 490f, 617f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 4º linea
            //VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "VEHICULO", 32f, 605f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, tipo_vehiculo, 75f, 605f, 0);
            cb.EndText();

            //MARCA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MARCA", 165f, 605f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, marca, 205f, 605f, 0);
            cb.EndText();

            //MODELO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MODELO", 315f, 605f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, modelo, 345f, 605f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 5º linea
            //PST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PTS.", 32f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, puertas, 52f, 593f, 0);
            cb.EndText();

            //AST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AST.", 85f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, asientos, 105f, 593f, 0);
            cb.EndText();

            //KG
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "KG.", 120f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 135f, 593f, 0);
            cb.EndText();

            //CHASIS
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CHASIS", 165f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, chasis, 205f, 593f, 0);
            cb.EndText();

            //MOTOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MOTOR", 315f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, motor, 345f, 593f, 0);
            cb.EndText();

            //AÑO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AÑO", 435f, 593f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, anio, 470f, 593f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 6º linea

            //CODIGO S.I.I.
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CODIGO S.I.I.", 32f, 581f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, codigo_sii ?? "", 90f, 581f, 0);
            cb.EndText();

            //TASACION
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TASACION", 165f, 581f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, tasacion, 205f, 581f, 0);
            cb.EndText();


            //COLOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COLOR", 315f, 581f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, color, 345f, 581f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 7º linea

            //PAGO EN CUOTAS
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PAGO EN CUOTAS", 32f, 569f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 95f, 569f, 0);
            cb.EndText();

            //PAGO TOTAL
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PAGO TOTAL", 165f, 569f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, pago_total, 205f, 569f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 8º linea

            //PERM. ANT.
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PERM. ANT.", 32f, 557f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            //cb.ShowTextAligned(Element.ALIGN_LEFT, texto29, 70f, 557f, 0);
            cb.EndText();

            //CORRECCION MONETARIA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MULTA", 315f, 557f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, multa, 390f, 557f, 0);
            cb.EndText();
            //--------------------------------------------------------------------- Definimos la 9º linea

            //C.C.
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "C.C.", 32f, 545f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, cilindrada, 58f, 545f, 0);
            cb.EndText();

            //TRM
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TRM.", 140f, 545f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, transmision, 160f, 545f, 0);
            cb.EndText();

            //I.P.C
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "I.P.C", 315f, 545f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, ipc, 420f - cb.GetEffectiveStringWidth(ipc, true), 545f, 0); //415f
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 10º linea

            //SELLO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "SELLO", 32f, 533f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, sello_verde, 58f, 533f, 0);
            cb.EndText();

            //MEDIO PAGO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MEDIO PAGO", 150f, 533f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 165f, 533f, 0);
            cb.EndText();

            //INTERESES
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "INTERESES", 315f, 533f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, intereses, 420f - cb.GetEffectiveStringWidth(intereses, true), 533f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 11º linea

            //TOTAL A PAGAR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL NETO", 315f, 521f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, total_neto, 420f - cb.GetEffectiveStringWidth(total_neto, true), 521f, 0);
            cb.EndText();

            //---------------------------------------------------------------------

            //TOTAL A PAGAR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL A PAGAR", 315f, 509f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, total_pagado, 420f - cb.GetEffectiveStringWidth(total_pagado, true), 509f, 0);
            cb.EndText();

            //---------------------------------------------2 tabla--------------------------------------------------------------

            //TITULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PERMISO DE CIRCULACION", 32f, 430f, 0);
            cb.EndText();

            //N° SERIE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "N° Serie", 420f, 430f, 0);
            cb.EndText();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(482f, 429f, 100f, 10f, 2f);
            cb.Fill();
            cb.RestoreState();

            cb.SaveState();
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.SetColorFill(new BaseColor(255, 0, 0));
            cb.ShowTextAligned(Element.ALIGN_LEFT, permiso.id_permisosCirculacion.ToString(), 530f, 430f, 0);
            cb.EndText();
            cb.RestoreState();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(30f, 412f, 268f, 12f, 2f);
            cb.Fill();
            cb.RestoreState();

            //Nombre municipalidad
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "I. Municipalidad de Paihuano", 32f, 415f, 0);
            cb.EndText();

            //AÑO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Periodo", 245f, 415f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, DateTime.Now.Year.ToString(), 280f, 415f, 0);
            cb.EndText();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(313f, 399f, 140f, 25f, 2f);
            cb.Fill();
            cb.RestoreState();

            //VALIDA HASTA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "VALIDO HASTA", 315f, 415f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, Funciones.fechaValida(fecha_vencimiento), 318f, 405f, 0);
            cb.EndText();

            //PLACA UNICA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PLACA UNICA", 395f, 415f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 12f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, patente + "-" + dv_patente, 383f, 402f, 0);
            cb.EndText();

            //--------------------------------------------------------------- Definimos la 2º linea

            //VECHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "VEHICULO", 32f, 399f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, tipo_vehiculo, 68f, 399f, 0);
            cb.EndText();

            //MARCA VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MARCA", 150f, 399, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, marca, 182f, 399, 0);
            cb.EndText();


            //AÑO VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AÑO", 260f, 399, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, anio, 280f, 399, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 3º linea

            //MODELO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MODELO", 32f, 381f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, modelo, 65f, 381f, 0);
            cb.EndText();


            //AÑO VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL PAGADO", 315f, 384f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, pago_total, 390f, 384f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 4º linea
            //COLOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COLOR", 32f, 363f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, color, 58f, 363f, 0);
            cb.EndText();

            //MOTOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MOTOR", 182f, 363f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, motor, 215f, 363f, 0);
            cb.EndText();

            //CODIGO SII
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CODIGO S.I.I", 315f, 365f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, codigo_sii ?? "", 390f, 365f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 5º linea
            //CARGA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CARGA", 32f, 349f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, carga, 58f, 349f, 0);
            cb.EndText();

            //AST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AST.", 95f, 349f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, asientos, 120f, 349f, 0);
            cb.EndText();

            //PST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PTS.", 150f, 349f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, puertas, 170f, 349f, 0);
            cb.EndText();

            //C.C.
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "C.C.", 315f, 349f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 335f, 349f, 0);
            cb.EndText();

            //COMBUSTIBLE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COMB.", 380f, 349f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, combustible, 410f, 349f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 6º linea

            //CONTRIBUYENTE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CONTRIBUYENTE", 32f, 333f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, nombre_propietario, 90f, 333f, 0);
            cb.EndText();

            //TRM
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TRM.", 315f, 333f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, transmision, 335f, 333f, 0);
            cb.EndText();


            //COMBUSTIBLE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "EQU.", 380f, 333f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, equipamiento, 410f, 333f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 7º linea

            //RUT
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "RUT", 32f, 317f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, rut, 65f, 317f, 0);
            cb.EndText();

            //RUT
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "FECHA EMISION", 150f, 317f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, fechaPermiso, 210f, 317f, 0);
            cb.EndText();

            //CUOTA 1
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 1", 315f, 317f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 335f, 317f, 0);
            cb.EndText();


            //CUOTA 2
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 2", 368f, 317f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 410f, 317f, 0);
            cb.EndText();


            //TOTAL
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL", 420f, 317f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 410f, 317f, 0);
            cb.EndText();

            //---------------------------------------------3 tabla--------------------------------------------------------------

            //TITULO                
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PERMISO DE CIRCULACION", 32f, 230f, 0);
            cb.EndText();

            //N° SERIE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "N° Serie", 420f, 230f, 0);
            cb.EndText();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(482f, 229f, 100f, 10f, 2f);
            cb.Fill();
            cb.RestoreState();

            cb.SaveState();
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.SetColorFill(new BaseColor(255, 0, 0));
            cb.ShowTextAligned(Element.ALIGN_LEFT, permiso.id_permisosCirculacion.ToString(), 530f, 230f, 0);
            cb.EndText();
            cb.RestoreState();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(30f, 212f, 268f, 12f, 2f);
            cb.Fill();
            cb.RestoreState();

            //Nombre municipalidad
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 9f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "I. Municipalidad de Paihuano", 32f, 215f, 0);
            cb.EndText();


            //AÑO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Periodo", 245f, 215f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, DateTime.Now.Year.ToString(), 280f, 215f, 0);
            cb.EndText();

            cb.SaveState();
            cb.SetRGBColorFill(222, 222, 222);
            cb.RoundRectangle(313f, 199f, 140f, 25f, 2f);
            cb.Fill();
            cb.RestoreState();

            //VALIDA HASTA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "VALIDO HASTA", 315f, 215f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, Funciones.fechaValida(fecha_vencimiento), 318f, 205f, 0);
            cb.EndText();

            //PLACA UNICA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 7f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PLACA UNICA", 395f, 215f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 12f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, patente + "-" + dv_patente, 383f, 202f, 0);
            cb.EndText();

            //--------------------------------------------------------------- Definimos la 2º linea

            //VECHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "VEHICULO", 32f, 199f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, tipo_vehiculo, 65f, 199f, 0);
            cb.EndText();

            //MARCA VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MARCA", 150f, 199f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, marca, 182f, 199f, 0);
            cb.EndText();


            //AÑO VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AÑO", 260f, 199f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, anio, 280f, 199f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 3º linea

            //COLOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MODELO", 32f, 183f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, modelo, 65f, 183f, 0);
            cb.EndText();


            //AÑO VEHICULO
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL PAGADO", 315f, 183f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, pago_total, 390f, 183f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 4º linea

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COLOR", 32f, 167f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, color, 58f, 167f, 0);
            cb.EndText();

            //MOTOR
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "MOTOR", 182f, 167f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, motor, 215f, 167f, 0);
            cb.EndText();

            //CODIGO SII
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CODIGO S.I.I", 315f, 167f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, codigo_sii ?? "", 390f, 167f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 5º linea

            //CARGA
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CARGA", 32f, 151f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, carga, 58f, 151f, 0);
            cb.EndText();

            //AST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "AST.", 95f, 151f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, asientos, 120f, 151f, 0);
            cb.EndText();

            //PST
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "PTS.", 150f, 151f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, puertas, 170f, 151f, 0);
            cb.EndText();

            //C.C.
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "C.C.", 315f, 151f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, cilindrada, 335f, 151f, 0);
            cb.EndText();

            //COMBUSTIBLE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "COMB.", 380f, 151f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, combustible, 410f, 151f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 6º linea

            //CONTRIBUYENTE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CONTRIBUYENTE", 32f, 135f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, nombre_propietario, 90f, 135f, 0);
            cb.EndText();

            //TRM
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TRM.", 315f, 135f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, transmision, 335f, 135f, 0);
            cb.EndText();


            //COMBUSTIBLE
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "EQU.", 380f, 135f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, equipamiento, 410f, 135f, 0);
            cb.EndText();

            //--------------------------------------------------------------------- Definimos la 7º linea

            //RUT
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "RUT", 32f, 119f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, rut, 65f, 119f, 0);
            cb.EndText();

            //RUT
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 6f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "FECHA EMISION", 150f, 119f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, fechaPermiso, 210f, 119f, 0);
            cb.EndText();

            //CUOTA 1
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 1", 315f, 119f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 335f, 119f, 0);
            cb.EndText();


            //CUOTA 2
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "CUOTA 2", 368f, 119f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 410f, 119f, 0);
            cb.EndText();

            //TOTAL
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, false), 5f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "TOTAL", 420f, 119f, 0);
            cb.EndText();

            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 8f);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "", 410f, 119f, 0);
            cb.EndText();


            doc.Close();
            writer.Close();

            return archivoPDF;
        }

    }
}
