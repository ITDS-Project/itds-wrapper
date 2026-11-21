using GameController;
using ITDSWrapper.Core;

namespace ITDSWrapper.iOS;

public class IosUpdater(IosControllerInputDriver inputDriver) : IUpdater
{
    public int Update()
    {
        if (GCController.Controllers.Length > 0)
        {
            if (GCController.Current?.ExtendedGamepad is null || GCController.Current.Equals(inputDriver.Controller))
            {
                return -1;
            }
            
            inputDriver.SetController(GCController.Controllers[0]);
            int returnValue = -1;
            GCController.Current!.ExtendedGamepad.ValueChangedHandler += (_, args) =>
            {
                inputDriver.Push(args);
                inputDriver.Release(args);
                returnValue = 1;
            };
            return returnValue;
        }
        return -1;
    }

    public void Die()
    {
    }
}