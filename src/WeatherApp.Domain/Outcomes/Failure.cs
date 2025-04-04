namespace WeatherApp.Domain.Outcomes;

public class Failure(OneOf<InvalidRequestFailure, 
                            UnsupportedRegionFailure, 
                            WeatherModelingServiceRejectionFailure,
                            ContributorPaymentServiceFailure> input)
    : OneOfBase<InvalidRequestFailure, 
                UnsupportedRegionFailure, 
                WeatherModelingServiceRejectionFailure,
                ContributorPaymentServiceFailure>(input)
{
    public static implicit operator Failure(InvalidRequestFailure failure) => new(failure);
    public static implicit operator Failure(UnsupportedRegionFailure failure) => new(failure);
    public static implicit operator Failure(WeatherModelingServiceRejectionFailure failure) => new(failure);
    public static implicit operator Failure(ContributorPaymentServiceFailure failure) => new(failure);
}