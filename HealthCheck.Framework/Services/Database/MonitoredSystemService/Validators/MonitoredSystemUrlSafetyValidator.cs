using System.Net;
using System.Net.Sockets;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;

public static class MonitoredSystemUrlSafetyValidator
{
    /// <summary>
    /// Verifica se a URL é segura para monitoramento, bloqueando URLs que apontam para recursos internos ou não web, <br/>
    /// como localhost, endereços IP privados, ou esquemas não HTTP/HTTPS, prevenindo ataques de SSRF (Server-Side Request Forgery) <br/>
    /// e garantindo que o monitoramento acesse apenas destinos externos legítimos.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static async Task<bool> IsAllowedAsync(string url)
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Valida se a URL é bem formada e utiliza os esquemas HTTP ou HTTPS, garantindo que
        //o monitoramento seja direcionado apenas a endpoints web legítimos.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return true;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Bloqueia URLs que utilizam esquemas não HTTP/HTTPS, como file://, fftp://, gopher://, etc.,
        //prevenindo tentativas de acesso a recursos locais ou não web.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Bloqueia URLs que apontam para localhost ou endereços IP privados,
        //evitando que o monitoramento acesse recursos internos da rede.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (uri.IsLoopback)
            return false;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Tenta resolver o host da URL para um endereço IP
        //e verifica se é um endereço IP bloqueado.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (IPAddress.TryParse(uri.Host, out var ipAddress))
            return !IsBlockedIp(ipAddress);

        IPAddress[] addresses;

        try
        {
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //Resolve o host da URL para um ou mais endereços IP e verifica se algum deles é um endereço IP bloqueado.
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            //******************************************************************************************************************************
            //- O que é resolução de DNS? É o processo de converter um nome de domínio (como www.exemplo.com)
            //em um endereço IP (como 192.168.1.1).
            //******************************************************************************************************************************
            //
            //******************************************************************************************************************************
            //- Para que serve? Para que os computadores possam se comunicar usando nomes de domínio amigáveis
            //em vez de endereços IP numéricos.
            //******************************************************************************************************************************
            //
            //******************************************************************************************************************************
            //- Como funciona? Quando um programa precisa acessar um recurso usando um nome de domínio,
            //ele consulta um servidor DNS para obter o endereço IP correspondente.
            //******************************************************************************************************************************
            //
            //******************************************************************************************************************************
            //- Por que é necessário aqui? Para garantir que o monitoramento acesse apenas destinos externos seguros,
            //evitando recursos internos ou maliciosos.
            //******************************************************************************************************************************

            addresses = await Dns.GetHostAddressesAsync(uri.Host);
        }
        catch
        {
            //Se a resolução de DNS falhar, bloqueia a URL por precaução, evitando que o monitoramento acesse destinos
            //potencialmente inseguros ou maliciosos.
            return false;
        }

        if (addresses.Length == 0)
            return false;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Verifica se algum dos endereços IP resolvidos é um endereço IP bloqueado.
        //Se algum endereço IP for bloqueado, a URL é considerada insegura e o monitoramento não deve acessá-la.
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        return !addresses.Any(IsBlockedIp);
    }

    private static bool IsBlockedIp(IPAddress ipAddress)
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Valida se o endereço IP é um endereço de loopback, como 127.0.0.1 ou ::1,
        //prevenindo que o monitoramento acesse recursos locais.
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (IPAddress.IsLoopback(ipAddress))
            return true;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Valida se o endereço IP é um endereço privado, como 10.x.x.x, 192.168.x.x, ou 172.16.x.x - 172.31.x.x,
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Valida se o endereço IPv6 é um endereço de link-local, site-local ou Unique Local Address (ULA),
        //prevenindo que o monitoramento acesse recursos locais ou endereços IPv6 não roteáveis.
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
                return true;

            var bytes = ipAddress.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }
}
