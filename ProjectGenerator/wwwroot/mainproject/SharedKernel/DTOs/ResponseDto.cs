using System.Collections.Generic;

namespace Attar.SharedKernel.DTOs;

public class Messages
{
    public string message { get; set; } = string.Empty;
}

public class ResponseDto
{
    public bool Success { get; set; }

    public int Code { get; set; }

    public List<Messages> Messages { get; set; } = new();
}

public class ResponseDto<T> : ResponseDto
{
    public T? Data { get; set; }
}
