// Apartment ownership
// By: McTwist (9845)
// Makes it possible to give apartments to other players without giving special restrictions

// These are only dummies, to make sure someone doesn't use it badly
function fxDTSBrick::setApartment(%brick){}
function Apartment::setOwner(%this){}

// Set events
// Using Apartment object as output to make an another tag
registerInputEvent(fxDTSBrick, setApartment, "And Apartment");
registerOutputEvent(Apartment, setOwner, "int 0 999999 0");

// Check if owner of the brick
function fxDTSBrick::isOwner(%brick, %bl_id)
{
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 && %ID <= 999999))
			continue;
		
		// Found it
		if (%ID $= %bl_id)
			return true;
	}
	return false;
}
// Check if got trust from owner
function fxDTSBrick::getOwnerTrust(%brick, %brickgroup, %quick)
{
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 && %ID <= 999999))
			continue;
		
		// Has trust, doesn't matter which direction we check
		if (%quick && %brickgroup.Trust[%ID] >= 1)
			return %brickgroup.Trust[%ID];
		// A more correct check
		else if (!%quick && isObject(%group = "BrickGroup_" @ %ID) && (%trust = getTrustLevel(%group, %brickgroup)) >= 1)
			return %trust;
	}
	return 0;
}
// Get first owner
function fxDTSBrick::getFirstOwner(%brick)
{
	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		if (%brick.eventInput[%i] !$= "setApartment" || %brick.eventOutput[%i] !$= "setOwner")
			continue;
		
		%ID = %brick.eventOutputParameter[%i, 1];
		
		// Validate
		if (!(%ID >= 0 && %ID <= 999999))
			continue;
		
		return %ID;
	}
	return -1;
}

// The real "magic"
package PackageApartment
{
	// When the brick is loaded
	function fxDTSBrick::onLoadPlant(%brick)
	{
		%ret = Parent::onLoadPlant(%brick);

		// New Duplicator placed it
		if (isObject(%brick.client))
			return %ret;
		
		%owner = -1;
		// Check bricks down
		for (%i = 0; %owner $= -1 && %i < %brick.getNumDownBricks(); %i++)
		{
			%next = %brick.getDownBrick(%i);
			%owner = %next.getFirstOwner();
		}
		// Found real owner
		if (%owner !$= -1)
		{
			%group = "BrickGroup_" @ %owner;
			// Create group
			if (!isObject(%group))
			{
				new SimGroup(%group)
				{
					bl_id = %owner;
					name = "\c1BL_ID: " @ %owner @ "\c1\c0";
					client = 0;
				};
				mainBrickGroup.add(%group);
			}
			// This is critical
			%group.schedule(0, add, %brick);
		}

		return %ret;
	}
	
	// Checking trust level
	function getTrustLevel(%obj1, %obj2)
	{
		// Check the bricks
		if (isObject(%obj1) && %obj1.getType() & $TypeMasks::FxBrickAlwaysObjectType
			&& isObject(%obj2) && %obj2.getType() & $TypeMasks::FxBrickAlwaysObjectType)
		{
			%group = %obj2.getGroup();
			// Is a renter
			if (%obj1.isOwner(%group.bl_id))
			{
				return 3;
			}

			// Get trust level
			if ((%trust = %obj1.getOwnerTrust(%group, false)) >= 1)
			{
				return %trust;
			}
		}
		return Parent::getTrustLevel(%obj1, %obj2);
	}

	// New Duplicator fast trust check
	// Made differently to ease the speed instead of versatility
	function ndFastTrustCheck(%brick, %bl_id, %brickgroup)
	{
		if (%brick.isOwner(%bl_id))
		{
			return true;
		}

		if (%brick.getOwnerTrust(%brickgroup, true) >= 1)
		{
			return true;
		}

		return Parent::ndFastTrustCheck(%brick, %bl_id, %brickgroup);
	}
};
activatePackage(PackageApartment);
