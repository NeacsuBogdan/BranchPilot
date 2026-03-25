using BranchPilot.Application.Customers;
using BranchPilot.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [Authorize(Policy = PermissionCodes.CustomersView)]
    [HttpGet]
    [ProducesResponseType(typeof(CustomerPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerPageResponse>> GetCustomersAsync(
        [FromQuery] GetCustomersRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetCustomersAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CustomersView)]
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetCustomerAsync(customerId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CustomersManage)]
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomerAsync(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _customerService.CreateCustomerAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CustomersManage)]
    [HttpPut("{customerId:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomerAsync(
        Guid customerId,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _customerService.UpdateCustomerAsync(customerId, request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CustomersManage)]
    [HttpDelete("{customerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        await _customerService.DeleteCustomerAsync(customerId, cancellationToken);
        return NoContent();
    }
}
