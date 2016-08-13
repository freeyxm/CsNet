﻿using System;
using CsNet.Collections;

namespace Test
{
    class RBTreeTest : BinaryTreeTest<RBTree<int, int>, RBTreeNode<int, int>>
    {
        public override void TestValidity()
        {
            TestValidity("", 1000, 1000);
        }

        protected override bool ValidTree(RBTree<int, int> tree)
        {
            if (!tree._ValidBalance())
                return false;

            return base.ValidTree(tree);
        }

        public override void TestPerformace()
        {
            int maxCount = 1000000;
            RBTree<int, int> tree = new RBTree<int, int>(maxCount);
            TestPerformace(tree, maxCount);
        }
    }
}
