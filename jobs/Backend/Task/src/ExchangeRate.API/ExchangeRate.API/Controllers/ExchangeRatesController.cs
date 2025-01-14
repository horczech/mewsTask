﻿using System.ComponentModel.DataAnnotations;
using ExchangeRate.Application;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRate.API.Controllers;

[ApiController]
[Route("api/v1/exchange-rates")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ExchangeRatesController(IExchangeRateProvider exchangeRateProvider) : ControllerBase
{
    private readonly IExchangeRateProvider _exchangeRateProvider = exchangeRateProvider;
    
    /// <summary>
    /// Returns the exchange rate of the specified currency.
    /// </summary>
    /// <param name="currencyCode">Three-letter ISO 4217 code of the currency (e.g. USD).</param>
    [HttpGet("{currencyCode}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DTOs.ExchangeRate))]
    public async Task<IActionResult> GetExchangeRate([FromRoute, Required] string currencyCode) {
        if (!IsValidCurrencyCode(currencyCode))
            return BadRequest($"Invalid currency code \"{currencyCode}\".");
            
        var exchangeRate = await _exchangeRateProvider.GetExchangeRate(new Domain.Currency(currencyCode));

        if (exchangeRate is null)
            return NotFound($"Exchange rate for \"{currencyCode}\" was not found.");
        
        return Ok(exchangeRate);
    }
    
    /// <summary>
    /// Returns the exchange rates of the specified currencies.
    /// </summary>
    /// <param name="currencyCodes">Comma separated string of three-letter ISO 4217 codes of the currencies (e.g. USD, EUR, AUD).</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DTOs.ExchangeRate>))]
    public async Task<IActionResult> GetExchangeRates([FromQuery, Required] string currencyCodes) {
        if (string.IsNullOrEmpty(currencyCodes))
            return BadRequest("At least one currency code must be specified.");
        
        var codes = currencyCodes.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim().ToUpper()).ToList();
        var invalidCodes = codes.Where(x =>! IsValidCurrencyCode(x)).ToList();
        if (invalidCodes.Any())
            return BadRequest($"The following currency codes are invalid: {string.Join(", ", invalidCodes)}.");

        var currencies = codes.Select(x => new Domain.Currency(x));
        var exchangeRates = await _exchangeRateProvider.GetExchangeRates(currencies);

        return Ok(exchangeRates);
    }

    //Note: could possibly come up with better validation e.g. listing all valid currency codes (but that list would have to be maintained and will potentially cause issues) 
    // or save the codes when fetching the data from the CNB and validate against that list
    //Note: The bigger apps can take advantage of MediatR's pipeline behaviors to handle validation in a more elegant way using FluentValidation library
    private static bool IsValidCurrencyCode(string currencyCode) => !string.IsNullOrEmpty(currencyCode) && currencyCode.Length == 3;
}